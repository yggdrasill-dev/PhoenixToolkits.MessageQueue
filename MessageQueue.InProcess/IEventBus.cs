namespace Valhalla.MessageQueue.InProcess;

internal interface IEventBus
{
	IObservable<InProcessMessage> MessageObservable { get; }
}
