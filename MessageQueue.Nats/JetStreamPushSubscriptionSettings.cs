using NATS.Client;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

internal record JetStreamPushSubscriptionSettings(
	PushSubscribeOptions SubscribeOptions,
	EventHandler<MsgHandlerEventArgs> EventHandler);
