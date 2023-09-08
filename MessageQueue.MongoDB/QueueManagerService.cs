using MongoDBMessageQueue = MongoDB.Messaging.MessageQueue;

namespace Valhalla.MessageQueue.MongoDB;

internal class QueueManagerService : Dictionary<string, MongoDBMessageQueue>
{
}
