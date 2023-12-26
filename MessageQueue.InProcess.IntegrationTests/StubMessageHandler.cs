using Valhalla.MessageQueue;

namespace MessageQueue.InProcess.IntegrationTests;

internal class StubMessageHandler(PromiseStore promiseStore) : IMessageHandler<ReadOnlyMemory<byte>>
{
    public ValueTask HandleAsync(
        string subject,
        ReadOnlyMemory<byte> data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var id = new Guid(data.Span);

        promiseStore.SetResult(id);

        return ValueTask.CompletedTask;
    }
}
