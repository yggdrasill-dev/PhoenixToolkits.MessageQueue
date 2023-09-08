using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.RabbitMQ.Configuration;

internal interface ISubscribeRegistration
{
	ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken);
}
