using NATS.Client;

namespace Valhalla.MessageQueue.Nats;

internal record NatsSubscriptionSettings
{
	public string Subject { get; set; } = default!;

	public EventHandler<MsgHandlerEventArgs> EventHandler { get; set; } = default!;
}
