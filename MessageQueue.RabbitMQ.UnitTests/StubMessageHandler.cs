using System.Text;
using Valhalla.MessageQueue;

namespace MessageQueue.RabbitMQ.UnitTests;

class StubMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
    private static readonly TaskCompletionSource<string> _CompletionSource = new();

    public ValueTask HandleAsync(
        string subject,
        ReadOnlyMemory<byte> data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        _CompletionSource.TrySetResult(Encoding.UTF8.GetString(data.Span));

        return ValueTask.CompletedTask;
    }

    internal static Task<string> GetResultAsync()
        => _CompletionSource.Task;
}
