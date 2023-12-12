using Valhalla.MessageQueue;

namespace MessageQueue.InProcess.IntegrationTests;

internal class StubMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
    private readonly PromiseStore m_PromiseStore;

    public StubMessageHandler(PromiseStore promiseStore)
    {
        m_PromiseStore = promiseStore ?? throw new ArgumentNullException(nameof(promiseStore));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var id = new Guid(data.Span);

        m_PromiseStore.SetResult(id);

        return ValueTask.CompletedTask;
    }
}
