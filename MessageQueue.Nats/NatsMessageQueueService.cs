using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class NatsMessageQueueService : INatsMessageQueueService
{
	private readonly INatsConnectionManager m_NatsConnectionManager;
	private readonly ILogger<NatsMessageQueueService> m_Logger;
	private readonly IReplyPromiseStore m_ReplyPromiseStore;
	private readonly string? m_SessionReplySubject;

	public NatsMessageQueueService(
		INatsConnectionManager natsConnectionManager,
		string? sessionReplySubject,
		IReplyPromiseStore replyPromiseStore,
		ILogger<NatsMessageQueueService> logger)
	{
		m_NatsConnectionManager = natsConnectionManager ?? throw new ArgumentNullException(nameof(natsConnectionManager));
		m_SessionReplySubject = sessionReplySubject;
		m_ReplyPromiseStore = replyPromiseStore ?? throw new ArgumentNullException(nameof(replyPromiseStore));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
		var reply = await m_NatsConnectionManager.Connection.RequestAsync<TMessage, TReply>(
			subject,
			data,
			headers,
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

	public IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider)
	{
		foreach (var registration in registrations)
			yield return ActivatorUtilities.CreateInstance<JetStreamMessageSender>(
				serviceProvider,
				registration.Glob,
				m_NatsConnectionManager.CreateJsContext());
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
		return m_NatsConnectionManager.Connection.PublishAsync(msg, cancellationToken: cancellationToken);
	}

	public async ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default)
	{
		var js = m_NatsConnectionManager.CreateJsContext();

		_ = await js.UpdateStreamAsync(
			config,
			cancellationToken).ConfigureAwait(false);
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

	public ValueTask<IDisposable> SubscribeAsync(INatsSubscribe settings, CancellationToken cancellationToken = default)
		=> settings.SubscribeAsync(m_NatsConnectionManager, cancellationToken);

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
