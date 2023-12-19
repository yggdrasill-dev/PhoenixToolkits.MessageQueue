using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats;

internal class JetStreamMessageExchange : IMessageExchange
{
	private readonly Glob m_Glob;
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public JetStreamMessageExchange(string pattern, INatsSerializerRegistry? natsSerializerRegistry)
	{
		m_Glob = Glob.Parse(pattern);
		m_NatsSerializerRegistry = natsSerializerRegistry;
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var connectionMgr = serviceProvider.GetRequiredService<INatsConnectionManager>();

		return connectionMgr.CreateJetStreamMessageSender(
			serviceProvider,
			m_NatsSerializerRegistry);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}
