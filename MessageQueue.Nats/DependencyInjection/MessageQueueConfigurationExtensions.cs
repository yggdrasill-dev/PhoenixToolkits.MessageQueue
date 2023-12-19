using NATS.Client.Core;
using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.Nats;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddNatsGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		INatsSerializerRegistry? natsSerializerRegistry = null)
		=> configuration.AddExchange(new NatsGlobMessageExchange(
			glob,
			configuration.SessionReplySubject,
			natsSerializerRegistry));

	public static MessageQueueConfiguration AddNatsJetStreamGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		INatsSerializerRegistry? natsSerializerRegistry = null)
		=> configuration
			.AddExchange(new JetStreamMessageExchange(
				glob,
				natsSerializerRegistry));
}
