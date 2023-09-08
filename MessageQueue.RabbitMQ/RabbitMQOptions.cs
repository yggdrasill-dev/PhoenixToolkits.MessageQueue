using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

public class RabbitMQOptions
{
	public Action<IModel> OnSetupQueueAndExchange { get; set; } = _ => { };

	public string Password { get; set; } = "guest";

	public Uri RabbitMQUrl { get; set; } = default!;

	public string UserName { get; set; } = "guest";

	public string VirtualHost { get; set; } = "/";
}
