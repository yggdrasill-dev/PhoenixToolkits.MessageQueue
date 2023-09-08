namespace Valhalla.MessageQueue.Configuration;

internal class MessageExchangeOptions
{
	public List<IMessageExchange> Exchanges { get; } = new List<IMessageExchange>();
}
