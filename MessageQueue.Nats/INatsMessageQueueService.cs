using NATS.Client.JetStream.Models;

namespace Valhalla.MessageQueue.Nats;

internal interface INatsMessageQueueService
	: IMessageReceiver<INatsSubscribe>
{
	ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default);
}
