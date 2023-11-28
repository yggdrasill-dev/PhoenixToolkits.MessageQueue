using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class JetStreamHandlerRegistration<THandler> : ISubscribeRegistration
	where THandler : INatsMessageHandler
{
	private readonly ConsumerConfiguration.ConsumerConfigurationBuilder m_ConsumerConfigurationBuilder;

	public JetStreamHandlerRegistration(ConsumerConfiguration.ConsumerConfigurationBuilder consumerConfigurationBuilder)
	{
		m_ConsumerConfigurationBuilder = consumerConfigurationBuilder ?? throw new ArgumentNullException(nameof(consumerConfigurationBuilder));
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		object receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> receiver is not IMessageReceiver<JetStreamPushSubscriptionSettings> messageReceiver
			? null
			: await messageReceiver.SubscribeAsync(new JetStreamPushSubscriptionSettings(
				m_ConsumerConfigurationBuilder.BuildPushSubscribeOptions(),
				(sender, args) => _ = JetStreamHandlerRegistration<THandler>.HandleMessageAsync(new MessageDataInfo
				{
					Args = args,
					ServiceProvider = serviceProvider,
					Logger = logger,
					CancellationToken = cancellationToken
				}).AsTask())).ConfigureAwait(false);

	private static async ValueTask HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(dataInfo.CancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.Message.Header,
			(header, key) => header[key],
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				dataInfo.Args.Message.Subject,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				})
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: dataInfo.Args.Message.Subject,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				});

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using var d = scope.ConfigureAwait(false);
			var handler = ActivatorUtilities.CreateInstance<THandler>(scope.ServiceProvider);
			var natsSender = scope.ServiceProvider.GetRequiredService<INatsMessageQueueService>();

			await handler.HandleAsync(
				dataInfo.Args,
				cts.Token).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", dataInfo.Args.Message.Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}
