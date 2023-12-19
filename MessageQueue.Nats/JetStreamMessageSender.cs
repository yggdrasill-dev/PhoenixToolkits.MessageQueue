using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class JetStreamMessageSender : IMessageSender
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;
	private readonly INatsJSContext m_NatsJSContext;
	private readonly ILogger<JetStreamMessageSender> m_Logger;

	public JetStreamMessageSender(
		INatsSerializerRegistry? natsSerializerRegistry,
		INatsJSContext natsJSContext,
		ILogger<JetStreamMessageSender> logger)
	{
		m_NatsSerializerRegistry = natsSerializerRegistry;
		m_NatsJSContext = natsJSContext ?? throw new ArgumentNullException(nameof(natsJSContext));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"Nats JetStream Publish");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var ack = m_NatsSerializerRegistry is null
			? await m_NatsJSContext.PublishAsync(
				subject,
				data,
				headers: MakeMsgHeader(appendHeaders),
				cancellationToken: cancellationToken).ConfigureAwait(false)
			: await m_NatsJSContext.PublishAsync(
				subject,
				data,
				headers: MakeMsgHeader(appendHeaders),
				serializer: m_NatsSerializerRegistry.GetSerializer<TMessage>(),
				cancellationToken: cancellationToken).ConfigureAwait(false);

		ack.EnsureSuccess();
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

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
