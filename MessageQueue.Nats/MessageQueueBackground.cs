using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class MessageQueueBackground : BackgroundService
{
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly IMessageReceiver<NatsSubscriptionSettings> m_MessageReceiver;
	private readonly IMessageReceiver<NatsQueueScriptionSettings> m_QueueReceiver;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_Subscribes;

	public MessageQueueBackground(
		IMessageReceiver<NatsSubscriptionSettings> messageReceiver,
		IMessageReceiver<NatsQueueScriptionSettings> queueReceiver,
		IServiceProvider serviceProvider,
		IEnumerable<ISubscribeRegistration> subscribes,
		ILogger<MessageQueueBackground> logger)
	{
		m_MessageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
		m_QueueReceiver = queueReceiver ?? throw new ArgumentNullException(nameof(queueReceiver));
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
					m_QueueReceiver,
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
