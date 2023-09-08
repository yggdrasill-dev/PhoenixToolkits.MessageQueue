using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class NoopMessageQueueServiceFactory : IMessageQueueServiceFactory
{
	public IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IModel channel)
		=> new NoopMessageQueueService();
}
