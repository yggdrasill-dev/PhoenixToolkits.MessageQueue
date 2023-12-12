namespace Valhalla.MessageQueue.Nats;

internal interface INatsSubscribe
{
	ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default);
}
