using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.RabbitMQ.Configuration;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class MessageQueueBackground(
	IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
	IServiceProvider serviceProvider,
	IEnumerable<ISubscribeRegistration> subscribes,
	RabbitMQConnectionManager rabbitMQConnectionManager,
	ILogger<MessageQueueBackground> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await rabbitMQConnectionManager.StartAsync(
			stoppingToken).ConfigureAwait(false);

		var subscriptions = new List<IDisposable>();

		foreach (var registration in subscribes)
			subscriptions.Add(
				await registration.SubscribeAsync(
					messageReceiver,
					serviceProvider,
					logger,
					stoppingToken).ConfigureAwait(false));

		_ = stoppingToken.Register(() =>
		{
			rabbitMQConnectionManager.StopAsync().Wait();

			foreach (var sub in subscriptions)
				sub.Dispose();
		});
	}
}
