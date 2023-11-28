using NATS.Client;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

internal record JetStreamPullSubscriptionSettings(
	PullSubscribeOptions SubscribeOptions,
	EventHandler<MsgHandlerEventArgs> EventHandler);