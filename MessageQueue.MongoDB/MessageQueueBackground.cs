using Microsoft.Extensions.Hosting;
using MongoDB.Messaging.Service;
using Valhalla.MessageQueue.MongoDB.Configuration;

namespace Valhalla.MessageQueue.MongoDB;

internal class MessageQueueBackground : BackgroundService
{
	private readonly Lazy<MessageService[]> m_QueueServices;

	public MessageQueueBackground(IEnumerable<MongoDBMessageQueueBuilder> builders, IServiceProvider serviceProvider)
	{
		m_QueueServices = new Lazy<MessageService[]>(() => builders
			.Select(builder => builder.Build(serviceProvider))
			.ToArray());
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var queueServices = m_QueueServices.Value;

		foreach (var svc in queueServices)
			svc.Start();

		_ = stoppingToken.Register(() =>
		{
			foreach (var svc in queueServices)
				svc.Stop();
		});

		return Task.CompletedTask;
	}
}
