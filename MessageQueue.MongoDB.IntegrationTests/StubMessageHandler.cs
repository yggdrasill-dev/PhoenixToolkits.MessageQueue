﻿using Valhalla.MessageQueue;

namespace MessageQueue.MongoDB.IntegrationTests;

internal class StubMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
