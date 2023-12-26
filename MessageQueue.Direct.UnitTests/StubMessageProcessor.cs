using Valhalla.MessageQueue;

namespace MessageQueue.Direct.UnitTests;

internal class StubMessageProcessor<TMessage, TReply> : IMessageProcessor<TMessage, TReply>
{
	public ValueTask<TReply> HandleAsync(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();
}
