namespace Valhalla.MessageQueue;

public interface IMessageHandler<in TMessage>
{
	ValueTask HandleAsync(string subject, TMessage data, CancellationToken cancellationToken = default);
}
