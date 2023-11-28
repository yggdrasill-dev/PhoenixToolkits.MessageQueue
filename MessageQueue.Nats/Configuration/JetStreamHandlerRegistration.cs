using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class JetStreamHandlerRegistration<THandler> : ISubscribeRegistration
	where THandler : INatsMessageHandler
{
	public string Subject { get; }

	public JetStreamHandlerRegistration(string subject)
	{
		Subject = subject;
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		object receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> receiver is not IMessageReceiver<JetStreamSubscriptionSettings> messageReceiver
			? null
			: await messageReceiver.SubscribeAsync(new JetStreamSubscriptionSettings(
				Subject,
				(sender, args) => _ = HandleMessageAsync(new MessageDataInfo
				{
					Args = args,
					ServiceProvider = serviceProvider,
					Logger = logger,
					CancellationToken = cancellationToken
				}).AsTask())).ConfigureAwait(false);

	private async ValueTask HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(dataInfo.CancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.Message.Header,
			(header, key) => header[key],
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				Subject,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				})
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
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
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}
