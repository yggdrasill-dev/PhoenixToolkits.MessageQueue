using Microsoft.Extensions.DependencyInjection.Extensions;
using Valhalla.MessageQueue.Exchanges;

namespace Valhalla.MessageQueue.Configuration;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddGlobPatternExchange<TMessageSender>(
		this MessageQueueConfiguration configuration,
		string glob)
		where TMessageSender : class, IMessageSender
		=> configuration.AddExchange(new GlobMessageExchange<TMessageSender>(glob));

	public static MessageQueueConfiguration AddNoopGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
	{
		configuration.Services.TryAddSingleton<NoopMessageSender>();
		return configuration.AddExchange(new GlobMessageExchange<NoopMessageSender>(glob));
	}
}
