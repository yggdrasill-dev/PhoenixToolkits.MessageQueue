namespace Valhalla.MessageQueue;

internal interface IEventBus
{
	IObservable<InProcessMessage> MessageObservable { get; }
}
