﻿using Valhalla.MessageQueue;

namespace MessageQueue.InProcess.UnitTests;

internal class StubMessageHandler<TMessage> : IMessageHandler<TMessage>
{
	public ValueTask HandleAsync(TMessage data, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}