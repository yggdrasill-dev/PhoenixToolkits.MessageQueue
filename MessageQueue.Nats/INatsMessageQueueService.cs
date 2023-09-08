namespace Valhalla.MessageQueue.Nats;

internal interface INatsMessageQueueService
	: IMessageSender, IMessageReceiver<NatsSubscriptionSettings>, IMessageReceiver<NatsQueueScriptionSettings>
{
}
