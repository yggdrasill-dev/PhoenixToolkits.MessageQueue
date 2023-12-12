using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class SessionRegistration<TMessage, TMessageSession> : ISubscribeRegistration
	where TMessageSession : IMessageSession<TMessage>
{
	private readonly bool m_IsSession;

	public string Subject { get; }

	public SessionRegistration(string subject, bool isSession = true)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;
		m_IsSession = isSession;
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new NatsSubscriptionSettings<TMessage>
			{
				Subject = Subject,
				EventHandler = (msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<TMessage>>(
						msg,
						logger,
						serviceProvider),
					ct)
			},
			cancellationToken).ConfigureAwait(false);

	private static async Task ProcessMessageAsync(
		Question<TMessage> question,
		TMessageSession handler,
		CancellationToken cancellationToken)
	{
		try
		{
			await handler
				.HandleAsync(question, cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception)
		{
			if (question.CanResponse)
				await question.FailAsync(
					"Process message occur error",
					cancellationToken).ConfigureAwait(false);

			throw;
		}
	}

	private Question<TMessage> CreateQuestion(MessageDataInfo<NatsMsg<TMessage>> dataInfo, INatsMessageQueueService natsSender)
		=> m_IsSession
			? new NatsQuestion<TMessage>(
				dataInfo.Msg.Data!,
				natsSender,
				dataInfo.Msg.ReplyTo)
			: new NatsAction<TMessage>(
				dataInfo.Msg.Data!);

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsMsg<TMessage>> dataInfo, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Msg.Headers,
			(header, key) => (header?[key] ?? string.Empty)!,
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				Subject,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				})
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				});

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var handler = ActivatorUtilities.CreateInstance<TMessageSession>(scope.ServiceProvider);
				var natsSender = scope.ServiceProvider.GetRequiredService<INatsMessageQueueService>();
				var question = CreateQuestion(dataInfo, natsSender);

				await ProcessMessageAsync(
					question,
					handler,
					cts.Token).ConfigureAwait(false);
			}
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
