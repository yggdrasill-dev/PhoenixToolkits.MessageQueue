using Valhalla.MessageQueue.MongoDB;
using Valhalla.MessageQueue;

namespace Valhalla.MessageQueue.MongoDB;

public interface IRabbitMQSerializerRegistry
{
	IRabbitMQSerializer<TMessage> GetSerializer<TMessage>();

	IRabbitMQDeserializer<TMessage> GetDeserializer<TMessage>();
}
