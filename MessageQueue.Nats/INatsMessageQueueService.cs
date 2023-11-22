using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal interface INatsMessageQueueService
	: IMessageSender
	, IMessageReceiver<NatsSubscriptionSettings>
	, IMessageReceiver<NatsQueueScriptionSettings>
	, IMessageReceiver<JetStreamSubscriptionSettings>
{
	void RegisterStream(string name, Action<StreamConfiguration.StreamConfigurationBuilder> streamConfigure);

	IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider);
}
