using Valhalla.MessageQueue;

namespace MessageQueue.MongoDB.IntegrationTests;

internal class StubMessageHandler : IMessageHandler
{
	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
