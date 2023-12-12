namespace Valhalla.MessageQueue;

public interface IMessageSession<TMessage>
{
	ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default);
}
