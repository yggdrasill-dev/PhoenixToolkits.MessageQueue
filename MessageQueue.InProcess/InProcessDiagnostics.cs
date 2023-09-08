using System.Diagnostics;

namespace Valhalla.MessageQueue;

internal static class InProcessDiagnostics
{
	public static ActivitySource ActivitySource = new("Valhalla.MessageQueue.InProcess");
}
