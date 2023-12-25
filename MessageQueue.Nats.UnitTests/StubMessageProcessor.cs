using Valhalla.MessageQueue;

namespace MessageQueue.Nats.UnitTests;

internal class StubMessageProcessor<TMessage, TReply> : IMessageProcessor<TMessage, TReply>
{
    public ValueTask<TReply> HandleAsync(
        string subject,
        TMessage data,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
