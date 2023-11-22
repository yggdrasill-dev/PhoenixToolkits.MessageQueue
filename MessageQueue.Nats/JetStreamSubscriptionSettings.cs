using NATS.Client;

namespace Valhalla.MessageQueue.Nats;

internal record JetStreamSubscriptionSettings(
	string Subject,
	EventHandler<MsgHandlerEventArgs> EventHandler);
