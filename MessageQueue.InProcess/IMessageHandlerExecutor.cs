namespace Valhalla.MessageQueue.InProcess;

internal interface IMessageHandlerExecutor
{
	ValueTask HandleAsync(InProcessMessage message, CancellationToken cancellationToken = default);
}
