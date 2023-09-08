using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.RabbitMQ.Configuration;

internal class SubscribeRegistration<THandler> : ISubscribeRegistration where THandler : IMessageHandler
{
	private static readonly Random _Random = new();
	private readonly bool m_AutoAck;
	private readonly int m_DispatchConcurrency;

	public string Subject { get; }

	public SubscribeRegistration(string subject, bool autoAck, int dispatchConcurrency)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));

		Subject = subject;
		m_AutoAck = autoAck;
		m_DispatchConcurrency = dispatchConcurrency;
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> messageReceiver.SubscribeAsync(new RabbitSubscriptionSettings
		{
			Subject = Subject,
			AutoAck = m_AutoAck,
			ConsumerDispatchConcurrency = m_DispatchConcurrency,
			EventHandler = (model, args) => HandleMessageAsync(new MessageDataInfo
			{
				Args = args,
				ServiceProvider = serviceProvider,
				Logger = logger,
				CancellationToken = cancellationToken,
				Channel = model
			})
		});

	private async Task HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.BasicProperties.Headers,
			(headers, key) => (headers[key] as string)!,
			out var context)
			? RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity(
				Subject,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "RabbitMQ"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				})
			: RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "RabbitMQ"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				});

		try
		{
#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
			await using var scope = dataInfo.ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait

			var handler = ActivatorUtilities.CreateInstance<THandler>(scope.ServiceProvider);

			await handler
				.HandleAsync(dataInfo.Args.Body.ToArray(), dataInfo.CancellationToken)
				.ConfigureAwait(false);

			if (!m_AutoAck)
				dataInfo.Channel.BasicAck(dataInfo.Args.DeliveryTag, false);
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			if (!m_AutoAck)
			{
				await Task.Delay(_Random.Next(1, 5) * 1000).ConfigureAwait(false);

				dataInfo.Channel.BasicNack(dataInfo.Args.DeliveryTag, false, true);
			}

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}
}
