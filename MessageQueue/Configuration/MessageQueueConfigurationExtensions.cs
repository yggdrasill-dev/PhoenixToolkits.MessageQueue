using Valhalla.MessageQueue.Exchanges;

namespace Valhalla.MessageQueue.Configuration;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddGlobPatternExchange<TMessageSender>(
		this MessageQueueConfiguration configuration,
		string glob)
		where TMessageSender : class, IMessageSender
		=> configuration.AddExchange(new GlobMessageExchange<TMessageSender>(glob));
}
