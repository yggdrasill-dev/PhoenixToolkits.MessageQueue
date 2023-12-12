namespace Valhalla.MessageQueue;

public interface IMessageProcessor<in TMessage, TReply>
{
	ValueTask<TReply> HandleAsync(TMessage data, CancellationToken cancellationToken = default);
}
