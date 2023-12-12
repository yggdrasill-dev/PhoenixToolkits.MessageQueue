using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

public interface INatsMessageHandler<TMessage>
{
	ValueTask HandleAsync(NatsJSMsg<TMessage> msg, CancellationToken cancellationToken = default);
}
