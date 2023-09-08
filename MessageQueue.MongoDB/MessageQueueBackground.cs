using Microsoft.Extensions.Hosting;
using MongoDB.Messaging.Service;
using Valhalla.MessageQueue.MongoDB.Configuration;

namespace Valhalla.MessageQueue.MongoDB;

internal class MessageQueueBackground : BackgroundService
{
	private readonly MessageService[] m_QueueServices;

	public MessageQueueBackground(IEnumerable<MongoDBMessageQueueBuilder> builders, IServiceProvider serviceProvider)
	{
		m_QueueServices = builders
			.Select(builder => builder.Build(serviceProvider))
			.ToArray();
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		foreach (var svc in m_QueueServices)
			svc.Start();

		_ = stoppingToken.Register(() =>
		{
			foreach (var svc in m_QueueServices)
				svc.Stop();
		});

		return Task.CompletedTask;
	}
}
