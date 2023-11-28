using NATS.Client;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

internal record JetStreamPushSubscriptionSettings(
	string Subject,
	PushSubscribeOptions SubscribeOptions,
	EventHandler<MsgHandlerEventArgs> EventHandler);
