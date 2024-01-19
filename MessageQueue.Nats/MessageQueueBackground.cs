using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream.Models;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class MessageQueueBackground : BackgroundService
{
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly INatsMessageQueueService m_NatsMessageQueueService;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_Subscribes;
	private readonly IEnumerable<StreamConfig> m_StreamRegistrations;

	public MessageQueueBackground(
		INatsMessageQueueService natsMessageQueueService,
		IServiceProvider serviceProvider,
		IEnumerable<ISubscribeRegistration> subscribes,
		IEnumerable<StreamConfig> streamRegistrations,
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

		foreach (var config in m_StreamRegistrations)
			await m_NatsMessageQueueService.RegisterStreamAsync(
				config,
				stoppingToken).ConfigureAwait(false);

		foreach (var registration in m_Subscribes)
		{
			var subsctiption = await registration.SubscribeAsync(
				m_NatsMessageQueueService,
				m_ServiceProvider,
				m_Logger,
				stoppingToken).ConfigureAwait(false);

			if (subsctiption is not null)
				subscriptions.Add(subsctiption);
		}

		m_Logger.LogDebug("Nats subscribe completed.");

		await stoppingToken.ConfigureAwait(false);

		foreach (var sub in subscriptions)
			sub.Dispose();
	}
}
