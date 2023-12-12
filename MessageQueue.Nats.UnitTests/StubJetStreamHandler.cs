using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats;

namespace MessageQueue.Nats.UnitTests;

internal class StubJetStreamHandler<TMessage> : INatsMessageHandler<TMessage>
{
    public ValueTask HandleAsync(NatsJSMsg<TMessage> msg, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
