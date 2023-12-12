using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class SessionReplyRegistration<TMessage> : ISubscribeRegistration
{
	public string Subject { get; }

	public SessionReplyRegistration(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' 不得為 Null 或空白字元。", nameof(subject));
		Subject = subject;
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
					new KeyValuePair<string, object?>("mq", "NATS")
				})
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS")
				});

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var store = scope.ServiceProvider.GetRequiredService<IReplyPromiseStore>();
				var replySender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
				var replyId = dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.SessionReplyKey, out var replyKeys) == true
					? replyKeys.FirstOrDefault()
					: null;

				dataInfo.Logger.LogInformation("ReplyId: {replyId}", replyId);

				if (replyId is not null && Guid.TryParse(replyId, out var replyGuid))
				{
					var responseData = dataInfo.Msg.Data;
					var replySubject = dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.SessionReplySubjectKey, out var replySubjects) == true
						? replySubjects.FirstOrDefault()
						: null;
					var askId = dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.SessionAskKey, out var askIds) == true
						? askIds.FirstOrDefault()
						: null;

					dataInfo.Logger.LogInformation("AskId: {askId}", askId);

					if (dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var failValues) == true
						&& failValues.Count > 0)
						store.SetException(replyGuid, new MessageProcessFailException(failValues.ToString()));
					else
						store.SetResult(
							replyGuid,
							new NatsAnswer<TMessage>(
								responseData!,
								replySender,
								replySubject,
								askId != null && Guid.TryParse(askId, out var askGuid) ? askGuid : null));
				}
			}
		}
		catch (Exception ex)
		{
			_ = activity?.AddTag("error", true);
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}
