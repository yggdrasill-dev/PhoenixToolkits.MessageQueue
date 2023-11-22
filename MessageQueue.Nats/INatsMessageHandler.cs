using NATS.Client;

namespace Valhalla.MessageQueue.Nats;

public interface INatsMessageHandler
{
	ValueTask HandleAsync(MsgHandlerEventArgs args, CancellationToken cancellationToken = default);
}
