using NATS.Client;

namespace Valhalla.MessageQueue.Nats;

internal record NatsQueueScriptionSettings
{
	public string Subject { get; set; } = default!;

	public string Queue { get; set; } = default!;

	public EventHandler<MsgHandlerEventArgs> EventHandler { get; set; } = default!;
}
