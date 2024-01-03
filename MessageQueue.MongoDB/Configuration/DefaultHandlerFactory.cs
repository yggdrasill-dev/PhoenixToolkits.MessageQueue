using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

internal static class DefaultHandlerFactory<THandler>
{
	public static Func<IServiceProvider, THandler> Default = sp => ActivatorUtilities.CreateInstance<THandler>(sp);
}
