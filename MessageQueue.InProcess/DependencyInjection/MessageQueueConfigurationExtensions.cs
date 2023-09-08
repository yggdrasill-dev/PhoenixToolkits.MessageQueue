using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddInProcessGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
		=> configuration.AddGlobPatternExchange<InProcessMessageQueue>(glob);
}
