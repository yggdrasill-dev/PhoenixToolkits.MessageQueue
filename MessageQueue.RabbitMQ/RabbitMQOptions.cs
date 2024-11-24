using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

public class RabbitMQOptions
{
	public Action<IModel> OnSetupQueueAndExchange { get; set; } = _ => { };

	public Func<ConnectionFactory> BuildConnectionFactory { get; set; } = () => new ConnectionFactory();
}
