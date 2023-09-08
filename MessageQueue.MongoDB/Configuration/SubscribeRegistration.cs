namespace Valhalla.MessageQueue.MongoDB.Configuration;

internal class SubscribeRegistration
{
	public Type HandlerType { get; init; } = default!;

	public TimeSpan PollTime { get; init; }

	public string QueueName { get; init; } = default!;

	public int Workers { get; init; }
}
