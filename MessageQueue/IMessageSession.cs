namespace Valhalla.MessageQueue;

public interface IMessageSession
{
	ValueTask HandleAsync(Question question, CancellationToken cancellationToken = default);
}
