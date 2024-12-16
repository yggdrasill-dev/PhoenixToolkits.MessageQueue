using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ;

record RabbitSubscriptionSettings
{
	public string Subject { get; init; } = default!;

	public bool AutoAck { get; init; } = true;

	public ushort ConsumerDispatchConcurrency { get; init; } = 1;

	public Func<IChannel, BasicDeliverEventArgs, Task> EventHandler { get; set; } = default!;
}
