using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal interface IMessageQueueServiceFactory
{
	IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IModel channel);
}
