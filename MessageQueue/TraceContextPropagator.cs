using System.Diagnostics;

namespace Valhalla.MessageQueue;

public static class TraceContextPropagator
{
	public static void Inject<T>(Activity? activity, T carrier, Action<T, string, string> setter)
	{
		try
		{
			DistributedContextPropagator.Current.Inject(activity, carrier, (c, k, v) => setter((T)c!, k, v));
		}
		catch
		{
		}
	}

	public static bool TryExtract<T>(T carrier, Func<T, string, string?> getter, out ActivityContext activityContext)
	{
		void ContextGetter(object? carrierObj, string name, out string? value, out IEnumerable<string>? values)
		{
			if (carrierObj is not T)
			{
				value = default;
				values = default;
				return;
			}

			value = getter(carrier, name);
			values = default;
		}

		try
		{
			activityContext = default;

			DistributedContextPropagator.Current.ExtractTraceIdAndState(
				carrier,
				ContextGetter,
				out string? traceParent,
				out string? traceState);
			activityContext = ActivityContext.TryParse(traceParent, traceState, out ActivityContext context) ? context : default;

			return activityContext != default;
		}
		catch
		{
		}

		return false;
	}
}
