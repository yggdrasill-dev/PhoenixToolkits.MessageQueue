using NATS.Client.JetStream.Models;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal interface INatsMessageQueueService
	: IMessageSender
	, IMessageReceiver<INatsSubscribe>
{
	ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default);

	IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider);
}
