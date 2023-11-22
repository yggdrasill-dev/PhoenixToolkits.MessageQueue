using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal interface ISubscribeRegistration
{
	ValueTask<IDisposable?> SubscribeAsync(
		object receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken);
}
