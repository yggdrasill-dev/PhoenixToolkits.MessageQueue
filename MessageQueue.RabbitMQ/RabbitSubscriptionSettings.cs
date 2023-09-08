using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ;

record RabbitSubscriptionSettings
{
	public string Subject { get; init; } = default!;

	public bool AutoAck { get; init; } = true;

	public int ConsumerDispatchConcurrency { get; init; } = 1;

	public Func<IModel, BasicDeliverEventArgs, Task> EventHandler { get; set; } = default!;
}
