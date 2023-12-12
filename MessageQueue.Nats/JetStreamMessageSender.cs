using DotNet.Globbing;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class JetStreamMessageSender : IMessageSender, IMessageExchange
{
	private readonly Glob m_Glob;
	private readonly INatsJSContext m_JetStream;
	private readonly ILogger<JetStreamMessageSender> m_Logger;

	public JetStreamMessageSender(string glob, INatsJSContext jetStream, ILogger<JetStreamMessageSender> logger)
	{
		m_Glob = Glob.Parse(glob);
		m_JetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
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

		var ack = await m_JetStream.PublishAsync(
			subject,
			data,
			headers: MakeMsgHeader(appendHeaders),
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

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
		=> this;

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);

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
