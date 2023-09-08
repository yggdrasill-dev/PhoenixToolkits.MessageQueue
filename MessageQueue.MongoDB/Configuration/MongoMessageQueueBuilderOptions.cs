using MongoDBMessageQueue = MongoDB.Messaging.MessageQueue;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

public sealed class MongoMessageQueueBuilderOptions
{
	public string ConnectionString { get; init; } = default!;

	public MongoDBMessageQueue MessageQueue { get; init; } = default!;
}
