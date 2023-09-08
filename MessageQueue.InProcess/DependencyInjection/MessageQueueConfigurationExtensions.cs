using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.InProcess;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddInProcessGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
		=> configuration.AddGlobPatternExchange<InProcessMessageQueue>(glob);
}
