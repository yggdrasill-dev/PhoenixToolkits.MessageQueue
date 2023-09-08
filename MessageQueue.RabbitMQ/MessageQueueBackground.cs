using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.RabbitMQ.Configuration;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class MessageQueueBackground : BackgroundService
{
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly IMessageReceiver<RabbitSubscriptionSettings> m_MessageReceiver;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_Subscribes;

	public MessageQueueBackground(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		IEnumerable<ISubscribeRegistration> subscribes,
		ILogger<MessageQueueBackground> logger)
	{
		m_MessageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Subscribes = subscribes ?? throw new ArgumentNullException(nameof(subscribes));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var subscriptions = new List<IDisposable>();

		foreach (var registration in m_Subscribes)
			subscriptions.Add(
				await registration.SubscribeAsync(
					m_MessageReceiver,
					m_ServiceProvider,
					m_Logger,
					stoppingToken).ConfigureAwait(false));

		_ = stoppingToken.Register(() =>
		{
			foreach (var sub in subscriptions)
				sub.Dispose();
		});
	}
}
