using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.RabbitMQ.Configuration;

internal class SubscribeRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private static readonly Random _Random = new();
	private readonly bool m_AutoAck;
	private readonly int m_DispatchConcurrency;
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public string Subject { get; }

	public SubscribeRegistration(
		string subject,
		bool autoAck,
		int dispatchConcurrency,
		Func<IServiceProvider, THandler> handlerFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));

		Subject = subject;
		m_AutoAck = autoAck;
		m_DispatchConcurrency = dispatchConcurrency;
		m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> messageReceiver.SubscribeAsync(
			new RabbitSubscriptionSettings
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
			},
			cancellationToken);

	private async Task HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(dataInfo.CancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.BasicProperties.Headers,
			(headers, key) => (headers[key] as string) ?? string.Empty,
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
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var handler = m_HandlerFactory(scope.ServiceProvider);
				var serializeRegistration = scope.ServiceProvider.GetRequiredService<IRabbitMQSerializerRegistry>();
				var deserializer = serializeRegistration.GetDeserializer<TMessage>();

				var msg = deserializer.Deserialize(new ReadOnlySequence<byte>(dataInfo.Args.Body));

				await handler.HandleAsync(
					dataInfo.Args.RoutingKey,
					msg!,
					dataInfo.Args.BasicProperties.Headers
						?.Select(kv => new MessageHeaderValue(kv.Key, kv.Value.ToString())),
					cts.Token)
					.ConfigureAwait(false);

				if (!m_AutoAck)
					dataInfo.Channel.BasicAck(dataInfo.Args.DeliveryTag, false);
			}
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
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}
