using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.MongoDB;
using Valhalla.MessageQueue.MongoDB.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static MessageQueueConfiguration AddMongoMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<MongoDBMessageQueueConfiguration> configure)
	{
		var mongoConfiguration = new MongoDBMessageQueueConfiguration(configuration);

		configure(mongoConfiguration);

		InitialRabbitMessageQueueConfiguration(mongoConfiguration);

		return configuration;
	}

	private static void InitialRabbitMessageQueueConfiguration(MongoDBMessageQueueConfiguration configuration)
		=> configuration.Services
			.AddHostedService<MessageQueueBackground>()
			.AddSingleton<QueueManagerService>()
			.AddTransient(typeof(MessageSubscriber<,>));
}
