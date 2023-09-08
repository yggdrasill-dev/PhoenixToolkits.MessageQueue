using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.MongoDB.Exchanges;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddMongoGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		string managerName,
		string queueName)
		=> configuration.AddExchange(new MongoDBGlobMessageExchange(glob, managerName, queueName));
}
