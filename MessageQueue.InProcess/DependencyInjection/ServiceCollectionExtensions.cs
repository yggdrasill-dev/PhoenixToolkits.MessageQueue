using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.InProcess;
using Valhalla.MessageQueue.InProcess.Configuration;

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
			.AddEventBus(configuration => configuration
				.RegisterEventSource<InProcessMessage>(EventBusNames.InProcessEventBusName))
			.AddSingleton<InProcessMessageQueue>()
			.AddHostedService<MessageQueueBackground>();

		return configuration;
	}
}
