using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class MessageQueueBackground : BackgroundService
{
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly INatsMessageQueueService m_NatsMessageQueueService;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_Subscribes;
	private readonly IEnumerable<StreamRegistration> m_StreamRegistrations;

	public MessageQueueBackground(
		INatsMessageQueueService natsMessageQueueService,
		IServiceProvider serviceProvider,
		IEnumerable<ISubscribeRegistration> subscribes,
		IEnumerable<StreamRegistration> streamRegistrations,
		ILogger<MessageQueueBackground> logger)
	{
		m_NatsMessageQueueService = natsMessageQueueService ?? throw new ArgumentNullException(nameof(natsMessageQueueService));
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Subscribes = subscribes ?? throw new ArgumentNullException(nameof(subscribes));
		m_StreamRegistrations = streamRegistrations ?? throw new ArgumentNullException(nameof(streamRegistrations));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var subscriptions = new List<IDisposable>();

		foreach (var registration in m_Subscribes)
			subscriptions.Add(
				await registration.SubscribeAsync(
					m_NatsMessageQueueService,
					m_NatsMessageQueueService,
					m_ServiceProvider,
					m_Logger,
					stoppingToken).ConfigureAwait(false));

		foreach (var registration in m_StreamRegistrations)
			m_NatsMessageQueueService.RegisterStream(registration.Name, registration.Configure);

		_ = stoppingToken.Register(() =>
		{
			foreach (var sub in subscriptions)
				sub.Dispose();
		});
	}
}
