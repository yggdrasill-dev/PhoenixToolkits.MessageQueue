using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.RabbitMQ.Exchanges;

internal class RabbitGlobMessageExchange : IMessageExchange
{
	private readonly string m_ExchangeName;
	private readonly Glob m_Glob;

	public RabbitGlobMessageExchange(string pattern, string exchangeName)
	{
		if (string.IsNullOrWhiteSpace(exchangeName))
			throw new ArgumentException($"'{nameof(exchangeName)}' 不得為 Null 或空白字元。", nameof(exchangeName));

		m_Glob = Glob.Parse(pattern);
		m_ExchangeName = exchangeName;
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var factory = serviceProvider.GetRequiredService<IMessageQueueServiceFactory>();
		var connectionManager = serviceProvider.GetRequiredService<RabbitMQConnectionManager>();

		return factory.CreateMessageQueueService(
			 serviceProvider,
			 m_ExchangeName,
			 connectionManager.MessageQueueChannel);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header) => m_Glob.IsMatch(subject);
}
