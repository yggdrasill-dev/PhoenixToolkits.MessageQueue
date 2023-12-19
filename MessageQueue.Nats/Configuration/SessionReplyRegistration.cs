using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class SessionReplyRegistration : ISubscribeRegistration
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public string Subject { get; }

	public SessionReplyRegistration(string subject, INatsSerializerRegistry? natsSerializerRegistry)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not null or empty.", nameof(subject));

		Subject = subject;
		m_NatsSerializerRegistry = natsSerializerRegistry;
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new NatsSubscriptionSettings<ReadOnlySequence<byte>>(
				Subject,
				(msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<ReadOnlySequence<byte>>>(
						msg,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<ReadOnlySequence<byte>>()),
			cancellationToken).ConfigureAwait(false);

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsMsg<ReadOnlySequence<byte>>> dataInfo, CancellationToken cancellationToken)
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

				dataInfo.Logger.LogDebug("ReplyId: {replyId}", replyId);

				if (replyId is not null && Guid.TryParse(replyId, out var replyGuid))
				{
					var responseData = dataInfo.Msg.Data;
					var replySubject = dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.SessionReplySubjectKey, out var replySubjects) == true
						? replySubjects.FirstOrDefault()
						: null;
					var askId = dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.SessionAskKey, out var askIds) == true
						? askIds.FirstOrDefault()
						: null;

					dataInfo.Logger.LogDebug("AskId: {askId}", askId);

					if (dataInfo.Msg.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var failValues) == true
						&& failValues.Count > 0)
						store.SetException(replyGuid, new MessageProcessFailException(failValues.ToString()));
					else
					{
						var replyType = store.GetPromiseType(replyGuid);
						if (replyType is null)
						{
							store.SetException(replyGuid, new MessageProcessFailException("Can't get promise reply type."));
							return;
						}

						var deserializeMethodInfo = typeof(SessionReplyRegistration).GetMethod(
							nameof(Deserialize),
							BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(replyType);

						_ = deserializeMethodInfo.Invoke(
							this,
							new object?[]{
								replyGuid,
								responseData,
								store,
								replySender,
								replySubject,
								askId != null && Guid.TryParse(askId, out var askGuid) ? askGuid : null
							});
					}
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

	private void Deserialize<TReply>(
		Guid replyGuid,
		ReadOnlySequence<byte> data,
		IReplyPromiseStore store,
		IMessageSender replySender,
		string? replySubject,
		Guid? askGuid)
	{
		var deserializer = (m_NatsSerializerRegistry ?? NatsDefaultSerializerRegistry.Default).GetDeserializer<TReply>();

		var response = deserializer.Deserialize(data);

		store.SetResult(
			replyGuid,
			new NatsAnswer<TReply>(
				response!,
				replySender,
				replySubject,
				askGuid));
	}
}
