namespace Valhalla.MessageQueue.InProcess;

internal static class EventBusNames
{
	public static string InProcessEventBusName = Guid.NewGuid().ToString();
}
