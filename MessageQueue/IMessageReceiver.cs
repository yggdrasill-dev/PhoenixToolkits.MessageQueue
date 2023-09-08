namespace Valhalla.MessageQueue;

public interface IMessageReceiver<TSubscriptionSettings>
{
	ValueTask<IDisposable> SubscribeAsync(TSubscriptionSettings settings);
}
