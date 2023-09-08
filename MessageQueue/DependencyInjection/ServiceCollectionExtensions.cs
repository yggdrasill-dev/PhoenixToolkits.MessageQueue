using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static MessageQueueConfiguration AddMessageQueue(this IServiceCollection services)
	{
		var configuration = new MessageQueueConfiguration(services);

		_ = services
			.AddSingleton<IReplyPromiseStore, ReplyPromiseStore>()
			.AddTransient<IMessageSender>(
				sp => new MultiplexerMessageSender(sp, configuration.GetRegisterExchanges()));

		return configuration;
	}
}
