namespace Valhalla.MessageQueue;

public interface IMessageHandler<in TMessage>
{
	ValueTask HandleAsync(TMessage data, CancellationToken cancellationToken = default);
}
