namespace Valhalla.MessageQueue.InProcess;

internal interface IMessageHandlerExecutor
{
	ValueTask HandleAsync(
		InProcessMessage message,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default);
}
