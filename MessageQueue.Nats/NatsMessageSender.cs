using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class NatsMessageSender : IMessageSender
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;
	private readonly string? m_SessionReplySubject;
	private readonly IReplyPromiseStore m_ReplyPromiseStore;
	private readonly ILogger<NatsMessageSender> m_Logger;
	private readonly INatsConnection m_Connection;

	public NatsMessageSender(
		INatsSerializerRegistry? natsSerializerRegistry,
		string? sessionReplySubject,
		INatsConnection connection,
		IReplyPromiseStore replyPromiseStore,
		ILogger<NatsMessageSender> logger)
	{
		m_NatsSerializerRegistry = natsSerializerRegistry;
		m_SessionReplySubject = sessionReplySubject;
		m_ReplyPromiseStore = replyPromiseStore ?? throw new ArgumentNullException(nameof(replyPromiseStore));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
	}

	public async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Ask");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		if (appendHeaders.Any(value => value.Name == MessageHeaderValueConsts.SessionAskKey))
			return await InternalAskAsync<TMessage, TReply>(
				subject,
				data,
				appendHeaders.Where(value => value.Name != MessageHeaderValueConsts.SessionAskKey),
				cancellationToken).ConfigureAwait(false);

		m_Logger.LogDebug("Ask");

		var headers = MakeMsgHeader(appendHeaders);

		if (!string.IsNullOrEmpty(m_SessionReplySubject))
			headers.Add(MessageHeaderValueConsts.SessionReplySubjectKey, m_SessionReplySubject);

		cancellationToken.ThrowIfCancellationRequested();
		var reply = m_NatsSerializerRegistry is null
			? await m_Connection.RequestAsync<TMessage, TReply>(
				subject,
				data,
				headers,
				cancellationToken: cancellationToken).ConfigureAwait(false)
			: await m_Connection.RequestAsync(
				subject,
				data,
				headers,
				requestSerializer: m_NatsSerializerRegistry.GetSerializer<TMessage>(),
				replySerializer: m_NatsSerializerRegistry.GetDeserializer<TReply>(),
				cancellationToken: cancellationToken).ConfigureAwait(false);

		if (reply.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var values) == true)
			throw new MessageProcessFailException(values.ToString());

		var replySubject = reply.Headers?.TryGetValue(MessageHeaderValueConsts.SessionReplySubjectKey, out var replySubjects) == true
			? replySubjects.FirstOrDefault()
			: null;
		var askId = reply.Headers?.TryGetValue(MessageHeaderValueConsts.SessionAskKey, out var askKeys) == true
			? askKeys.FirstOrDefault()
			: null;
		var askGuid = askId != null && Guid.TryParse(askId, out var id)
			? (Guid?)id
			: null;

		return new NatsAnswer<TReply>(
			reply.Data!,
			this,
			replySubject,
			askGuid);
	}

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Publish");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var msg = new NatsMsg<TMessage>
		{
			Subject = subject,
			Data = data,
			Headers = MakeMsgHeader(appendHeaders)
		};

		cancellationToken.ThrowIfCancellationRequested();
		return m_NatsSerializerRegistry is null
			? m_Connection.PublishAsync(msg, cancellationToken: cancellationToken)
			: m_Connection.PublishAsync(
				msg,
				serializer: m_NatsSerializerRegistry.GetSerializer<TMessage>(),
				cancellationToken: cancellationToken);
	}

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Request");

		var answer = await AskAsync<TMessage, TReply>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync("Send can't complete.", cancellationToken)
				.ConfigureAwait(false);

		return answer.Result;
	}

	public async ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Send");

		var answer = await AskAsync<TMessage, ReadOnlyMemory<byte>>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync("Send can't complete.", cancellationToken)
				.ConfigureAwait(false);
	}

	internal async ValueTask<Answer<TReply>> InternalAskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Internal Ask");

		m_Logger.LogInformation("Internal Ask: {subject}", subject);
		var (id, promise) = m_ReplyPromiseStore.CreatePromise<TReply>(cancellationToken);

		var appendHeaders = new List<MessageHeaderValue>();

		if (!string.IsNullOrEmpty(m_SessionReplySubject))
			appendHeaders.Add(new MessageHeaderValue(MessageHeaderValueConsts.SessionReplySubjectKey, m_SessionReplySubject));

		appendHeaders.Add(new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, id.ToString()));

		await PublishAsync(
			subject,
			data,
			header.Concat(appendHeaders),
			cancellationToken).ConfigureAwait(false);

		return await promise.ConfigureAwait(false);
	}

	private NatsHeaders MakeMsgHeader(IEnumerable<MessageHeaderValue> header)
	{
		ArgumentNullException.ThrowIfNull(header, nameof(header));

		var msgHeader = new NatsHeaders();
		foreach (var headerValue in header)
		{
			msgHeader.Add(headerValue.Name, headerValue.Value);
			m_Logger.LogDebug("Header: {headerValue.Name} = {headerValue.Value}", headerValue.Name, headerValue.Value);
		}

		return msgHeader;
	}
}
