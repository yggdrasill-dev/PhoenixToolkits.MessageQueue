using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client;
using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class NatsMessageQueueService : INatsMessageQueueService
{
	private readonly IConnection m_Connection;
	private readonly ILogger<NatsMessageQueueService> m_Logger;
	private readonly IReplyPromiseStore m_ReplyPromiseStore;
	private readonly string? m_SessionReplySubject;

	public NatsMessageQueueService(
		IConnection connection,
		string? sessionReplySubject,
		IReplyPromiseStore replyPromiseStore,
		ILogger<NatsMessageQueueService> logger)
	{
		m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_SessionReplySubject = sessionReplySubject;
		m_ReplyPromiseStore = replyPromiseStore ?? throw new ArgumentNullException(nameof(replyPromiseStore));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<Answer> AskAsync(
		string subject,
		ReadOnlyMemory<byte> data,
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
			return await InternalAskAsync(
				subject,
				data,
				appendHeaders.Where(value => value.Name != MessageHeaderValueConsts.SessionAskKey),
				cancellationToken).ConfigureAwait(false);

		m_Logger.LogDebug("Ask");
		var msg = new Msg(subject, data.ToArray())
		{
			Header = MakeMsgHeader(appendHeaders)
		};

		if (!string.IsNullOrEmpty(m_SessionReplySubject))
			msg.Header.Add(MessageHeaderValueConsts.SessionReplySubjectKey, m_SessionReplySubject);

		cancellationToken.ThrowIfCancellationRequested();
		var reply = await m_Connection.RequestAsync(msg, cancellationToken).ConfigureAwait(false);

		var responseData = reply.HasHeaders
			&& reply.Header.GetValues(NatsMessageHeaderValueConsts.FailMessageHeaderValue.Name)?.Length > 0
				? throw new MessageProcessFailException(reply.Data)
				: reply.Data;
		var replySubject = reply.HasHeaders
			? reply.Header.GetValues(MessageHeaderValueConsts.SessionReplySubjectKey)?.FirstOrDefault()
			: null;
		var askId = reply.HasHeaders
			? reply.Header.GetValues(MessageHeaderValueConsts.SessionAskKey)?.FirstOrDefault()
			: null;
		var askGuid = askId != null && Guid.TryParse(askId, out var id)
			? (Guid?)id
			: null;

		return new NatsAnswer(
			responseData,
			this,
			replySubject,
			askGuid);
	}

	public ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
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

		var msg = new Msg(subject, data.ToArray())
		{
			Header = MakeMsgHeader(appendHeaders)
		};

		cancellationToken.ThrowIfCancellationRequested();
		m_Connection.Publish(msg);

		return ValueTask.CompletedTask;
	}

	public async ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Request");

		var answer = await AskAsync(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync(Encoding.UTF8.GetBytes("Send can't complete."), cancellationToken)
				.ConfigureAwait(false);

		return answer.Result;
	}

	public async ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Send");

		var answer = await AskAsync(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync(Encoding.UTF8.GetBytes("Send can't complete."), cancellationToken)
				.ConfigureAwait(false);
	}

	public ValueTask<IDisposable> SubscribeAsync(NatsQueueScriptionSettings settings)
	{
		var subscription = m_Connection.SubscribeAsync(settings.Subject, settings.Queue, settings.EventHandler);

		return ValueTask.FromResult<IDisposable>(subscription);
	}

	public ValueTask<IDisposable> SubscribeAsync(NatsSubscriptionSettings settings)
	{
		var subscription = m_Connection.SubscribeAsync(settings.Subject, settings.EventHandler);

		return ValueTask.FromResult<IDisposable>(subscription);
	}

	public void RegisterStream(Action<StreamConfiguration.StreamConfigurationBuilder> streamConfigure)
	{
		var jsm = m_Connection.CreateJetStreamManagementContext();
		var builder = new StreamConfiguration.StreamConfigurationBuilder();
		streamConfigure(builder);

		var options = builder.Build();

		if (!jsm.GetStreamNames().Contains(options.Name))
			jsm.AddStream(options);
	}

	public IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider)
	{
		foreach (var registration in registrations)
			yield return ActivatorUtilities.CreateInstance<JetStreamMessageSender>(
				serviceProvider,
				registration.Glob,
				m_Connection.CreateJetStreamContext());
	}

	public ValueTask<IDisposable> SubscribeAsync(JetStreamPushSubscriptionSettings settings)
	{
		var js = m_Connection.CreateJetStreamContext();

		return string.IsNullOrEmpty(settings.SubscribeOptions.DeliverGroup)
			? ValueTask.FromResult((IDisposable)js.PushSubscribeAsync(
				settings.Subject,
				settings.EventHandler,
				false,
				settings.SubscribeOptions))
			: ValueTask.FromResult((IDisposable)js.PushSubscribeAsync(
				settings.Subject,
				settings.SubscribeOptions.DeliverGroup,
				settings.EventHandler,
				false,
				settings.SubscribeOptions));
	}

	internal async ValueTask<Answer> InternalAskAsync(
				string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats Internal Ask");

		m_Logger.LogInformation("Internal Ask: {subject}", subject);
		var (id, promise) = m_ReplyPromiseStore.CreatePromise(cancellationToken);

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

	private MsgHeader MakeMsgHeader(IEnumerable<MessageHeaderValue> header)
	{
		if (header is null)
			throw new ArgumentNullException(nameof(header));

		var msgHeader = new MsgHeader();
		foreach (var headerValue in header)
		{
			msgHeader.Add(headerValue.Name, headerValue.Value);
			m_Logger.LogDebug("Header: {headerValue.Name} = {headerValue.Value}", headerValue.Name, headerValue.Value);
		}

		return msgHeader;
	}
}
