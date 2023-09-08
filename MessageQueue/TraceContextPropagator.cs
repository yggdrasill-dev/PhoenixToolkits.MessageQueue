using System.Diagnostics;

namespace Valhalla.MessageQueue;

public static class TraceContextPropagator
{
	public const string TraceParent = "traceparent";

	public const string TraceState = "tracestate";

	public static void Inject<T>(Activity? activity, T carrier, Action<T, string, string> setter)
	{
		try
		{
			if (activity?.IdFormat == ActivityIdFormat.W3C && !string.IsNullOrEmpty(activity?.Id))
				setter(carrier, TraceParent, activity.Id);

			if (!string.IsNullOrEmpty(activity?.TraceStateString))
				setter(carrier, TraceState, activity.TraceStateString);
		}
		catch
		{
		}
	}

	public static bool TryExtract<T>(T carrier, Func<T, string, string> getter, out ActivityContext activityContext)
	{
		try
		{
			var traceParent = getter(carrier, TraceParent);
			var traceState = getter(carrier, TraceState);

			if (!string.IsNullOrEmpty(traceParent)
				&& ActivityContext.TryParse(traceParent, traceState, out var traceContext))
			{
				activityContext = traceContext;

				return true;
			}
		}
		catch
		{
		}

		return false;
	}
}
