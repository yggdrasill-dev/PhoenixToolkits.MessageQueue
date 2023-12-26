﻿using Valhalla.MessageQueue;

namespace MessageQueue.Nats.UnitTests;

internal class StubMessageHandler<TMessage> : IMessageHandler<TMessage>
{
    public ValueTask HandleAsync(
        string subject,
        TMessage data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default) 
        => throw new NotImplementedException();
}
