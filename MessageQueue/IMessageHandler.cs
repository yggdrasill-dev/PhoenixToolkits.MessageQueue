namespace Valhalla.MessageQueue;

public interface IMessageHandler
{
	ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
