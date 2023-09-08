using MongoDB.Messaging;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

internal record QueueDeclare(string Name, MessagePriority Priority = MessagePriority.Normal, int Retry = 0);
