using System.Diagnostics;

namespace Valhalla.MessageQueue.Direct;

internal class DirectDiagnostics
{
	public static ActivitySource ActivitySource = new("Valhalla.MessageQueue.Direct");
}
