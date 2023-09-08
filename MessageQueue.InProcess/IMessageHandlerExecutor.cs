namespace Valhalla.MessageQueue;

internal interface IMessageHandlerExecutor
{
	ValueTask HandleAsync(InProcessMessage message, CancellationToken cancellationToken = default);
}
