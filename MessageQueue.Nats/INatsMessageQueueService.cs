using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal interface INatsMessageQueueService
	: IMessageSender
	, IMessageReceiver<NatsSubscriptionSettings>
	, IMessageReceiver<NatsQueueScriptionSettings>
	, IMessageReceiver<JetStreamPushSubscriptionSettings>
{
	void RegisterStream(Action<StreamConfiguration.StreamConfigurationBuilder> streamConfigure);

	IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider);
}
