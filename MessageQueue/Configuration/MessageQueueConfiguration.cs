using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Valhalla.MessageQueue.Configuration;

public class MessageQueueConfiguration
{
	private readonly OptionsBuilder<MessageExchangeOptions> m_ExchangeOptionsBuilder;
	public IServiceCollection Services { get; }

	public string? SessionReplySubject { get; private set; }

	public MessageQueueConfiguration(IServiceCollection services)
	{
		Services = services ?? throw new ArgumentNullException(nameof(services));
		m_ExchangeOptionsBuilder = services.AddOptions<MessageExchangeOptions>();
	}

	public MessageQueueConfiguration AddExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Add(exchange));

		return this;
	}

	public MessageQueueConfiguration ClearAllExchanges()
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Clear());

		return this;
	}

	public MessageQueueConfiguration PushExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Insert(0, exchange));

		return this;
	}

	public MessageQueueConfiguration RegisterSessionReplySubject(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' 不得為 Null 或空白字元。", nameof(subject));
		SessionReplySubject = subject;

		return this;
	}

	public MessageQueueConfiguration RemoveExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Remove(exchange));

		return this;
	}
}
