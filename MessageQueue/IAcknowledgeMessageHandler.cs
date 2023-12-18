namespace Valhalla.MessageQueue;

public interface IAcknowledgeMessageHandler<TMessageHandler>
{
	ValueTask HandleAsync(IAcknowledgeMessage<TMessageHandler> msg, CancellationToken cancellationToken = default);
}
