using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static MessageQueueConfiguration AddInProcessMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<InProcessMessageQueueConfiguration> configure)
	{
		var inProcessConfiguration = new InProcessMessageQueueConfiguration(configuration);

		configure(inProcessConfiguration);

		_ = configuration.Services
			.AddSingleton<InProcessMessageQueue>()
			.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InProcessMessageQueue>())
			.AddHostedService<MessageQueueBackground>();

		return configuration;
	}
}
