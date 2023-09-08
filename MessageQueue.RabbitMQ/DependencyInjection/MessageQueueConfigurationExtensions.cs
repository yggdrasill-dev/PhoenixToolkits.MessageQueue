using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.RabbitMQ.Exchanges;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		string exchangeName)
		=> configuration.AddExchange(new RabbitGlobMessageExchange(glob, exchangeName));
}
