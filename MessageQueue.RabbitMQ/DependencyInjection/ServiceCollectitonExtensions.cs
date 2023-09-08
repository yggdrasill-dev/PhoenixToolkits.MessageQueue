using Microsoft.Extensions.Hosting;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.RabbitMQ;
using Valhalla.MessageQueue.RabbitMQ.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectitonExtensions
{
	public static IServiceCollection AddFakeRabbitMessageQueue(
		this IServiceCollection services)
	{
		foreach (var desc in services.Where(
			desc => desc.ServiceType == typeof(IHostedService)
				&& desc.ImplementationType == typeof(MessageQueueBackground)).ToArray())
		{
			_ = services.Remove(desc);
		}

		return services
			.AddSingleton<IMessageQueueServiceFactory, NoopMessageQueueServiceFactory>();
	}

	public static IServiceCollection AddRabbitMessageQueue(
			this IServiceCollection services,
		Action<RabbitMessageQueueConfiguration> configure)
	{
		var configuration = new RabbitMessageQueueConfiguration(services);

		configure(configuration);

		InitialRabbitMessageQueueConfiguration(configuration);

		return services;
	}

	public static MessageQueueConfiguration AddRabbitMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<RabbitMessageQueueConfiguration> configure)
	{
		var rabbitConfiguration = new RabbitMessageQueueConfiguration(configuration);

		configure(rabbitConfiguration);

		InitialRabbitMessageQueueConfiguration(rabbitConfiguration);

		return configuration;
	}

	private static void InitialRabbitMessageQueueConfiguration(RabbitMessageQueueConfiguration configuration)
		=> configuration.Services
			.AddSingleton<RabbitMQConnectionManager>()
			.AddSingleton<IMessageQueueServiceFactory, RabbitMessageQueueServiceFactory>()
			.AddSingleton<IMessageReceiver<RabbitSubscriptionSettings>, RabbitMQConnectionManager>(
				sp => sp.GetRequiredService<RabbitMQConnectionManager>())
			.AddHostedService<MessageQueueBackground>()
			.AddOptions<RabbitMQOptions>();
}
