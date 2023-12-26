using Valhalla.MessageQueue;

namespace MessageQueue.MongoDB.IntegrationTests;

internal class StubMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
    public ValueTask HandleAsync(
        string subject,
        ReadOnlyMemory<byte> data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
