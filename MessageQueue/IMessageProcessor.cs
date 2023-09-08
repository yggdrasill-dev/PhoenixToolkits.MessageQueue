namespace Valhalla.MessageQueue;

public interface IMessageProcessor
{
	ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
