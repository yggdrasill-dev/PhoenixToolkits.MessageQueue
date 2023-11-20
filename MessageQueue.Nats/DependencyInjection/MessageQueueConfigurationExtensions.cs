using Microsoft.Extensions.DependencyInjection.Extensions;
using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.Nats;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddNatsGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
		=> configuration.AddGlobPatternExchange<INatsMessageQueueService>(glob);

	public static MessageQueueConfiguration AddNatsJetStreamGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
	{
		configuration.Services
			.AddSingleton(new JetStreamExchangeRegistration(glob))
			.TryAddSingleton<JetStreamMessageExchange>();

		return configuration
			.AddGlobPatternExchange<JetStreamMessageExchange>(glob);
	}
}
