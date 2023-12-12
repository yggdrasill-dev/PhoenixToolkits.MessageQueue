using Valhalla.MessageQueue;

namespace MessageQueue.Direct.UnitTests;

internal class StubMessageProcessor<TMessage, TReply> : IMessageProcessor<TMessage, TReply>
{
	public ValueTask<TReply> HandleAsync(TMessage data, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
