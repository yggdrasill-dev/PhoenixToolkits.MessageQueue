using Microsoft.Extensions.Hosting;
using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.Nats;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFakeNatsMessageQueue(this IServiceCollection services)
	{
		foreach (var desc in services.Where(
			desc => desc.ServiceType == typeof(IHostedService)
				&& desc.ImplementationType == typeof(MessageQueueBackground)).ToArray())
			_ = services.Remove(desc);

		return services
			.AddSingleton<NoopMessageQueueService>()
			.AddSingleton<INatsConnectionManager, NoopConnectionManager>();
	}

	public static MessageQueueConfiguration AddNatsMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<NatsMessageQueueConfiguration> configure)
	{
		var natsConfiguration = new NatsMessageQueueConfiguration(configuration);

		configure(natsConfiguration);

		return configuration;
	}
}
