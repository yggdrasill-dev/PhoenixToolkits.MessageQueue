using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats;

internal class NatsGlobMessageExchange : IMessageExchange
{
	private readonly Glob m_Glob;
	private readonly string? m_SessionReplySubject;
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public NatsGlobMessageExchange(string pattern, string? sessionReplySubject, INatsSerializerRegistry? natsSerializerRegistry)
	{
		m_Glob = Glob.Parse(pattern);
		m_SessionReplySubject = sessionReplySubject;
		m_NatsSerializerRegistry = natsSerializerRegistry;
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var connectionMgr = serviceProvider.GetRequiredService<INatsConnectionManager>();

		return connectionMgr.CreateMessageSender(
			serviceProvider,
			m_NatsSerializerRegistry,
			m_SessionReplySubject);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}
