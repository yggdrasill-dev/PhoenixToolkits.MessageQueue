using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class SessionReplyRegistration : ISubscribeRegistration
{
	public string Subject { get; }

	public SessionReplyRegistration(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' 不得為 Null 或空白字元。", nameof(subject));
		Subject = subject;
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<NatsSubscriptionSettings> messageReceiver,
		IMessageReceiver<NatsQueueScriptionSettings> queueReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> messageReceiver.SubscribeAsync(new NatsSubscriptionSettings
		{
			Subject = Subject,
			EventHandler = (sender, args) => HandleMessageAsync(new MessageDataInfo
			{
				Args = args,
				ServiceProvider = serviceProvider,
				Logger = logger,
				CancellationToken = cancellationToken
			}).AsTask()
		});

	private async ValueTask HandleMessageAsync(MessageDataInfo dataInfo)
	{
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
#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
			await using var scope = dataInfo.ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
			var store = scope.ServiceProvider.GetRequiredService<IReplyPromiseStore>();
			var replySender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
			var replyId = dataInfo.Args.Message.HasHeaders
				? dataInfo.Args.Message.Header.GetValues(MessageHeaderValueConsts.SessionReplyKey)?.FirstOrDefault()
				: null;

			dataInfo.Logger.LogInformation("ReplyId: {replyId}", replyId);

			if (replyId is not null && Guid.TryParse(replyId, out var replyGuid))
			{
				var isFail = dataInfo.Args.Message.HasHeaders
					&& dataInfo.Args.Message.Header.GetValues(NatsMessageHeaderValueConsts.FailMessageHeaderValue.Name)?.Length > 0;
				var responseData = dataInfo.Args.Message.Data;
				var replySubject = dataInfo.Args.Message.HasHeaders
					? dataInfo.Args.Message.Header.GetValues(MessageHeaderValueConsts.SessionReplySubjectKey)?.FirstOrDefault()
					: null;
				var askId = dataInfo.Args.Message.HasHeaders
					? dataInfo.Args.Message.Header.GetValues(MessageHeaderValueConsts.SessionAskKey)?.FirstOrDefault()
					: null;

				dataInfo.Logger.LogInformation("AskId: {askId}", askId);

				if (isFail)
					store.SetException(replyGuid, new MessageProcessFailException(dataInfo.Args.Message.Data));
				else
					store.SetResult(
						replyGuid,
						new NatsAnswer(
							responseData,
							replySender,
							replySubject,
							askId != null && Guid.TryParse(askId, out var askGuid) ? askGuid : null));
			}
		}
		catch (Exception ex)
		{
			_ = activity?.AddTag("error", true);
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}
}
