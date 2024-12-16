using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitMessageQueueServiceFactory : IMessageQueueServiceFactory
{
	public IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IChannel channel)
		=> ActivatorUtilities.CreateInstance<RabbitMessageQueueService>(serviceProvider, exchangeName, channel);
}
