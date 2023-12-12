namespace Valhalla.MessageQueue.RabbitMQ;

public interface IRabbitMQSerializerRegistry
{
	IRabbitMQSerializer<TMessage> GetSerializer<TMessage>();

	IRabbitMQDeserializer<TMessage> GetDeserializer<TMessage>();
}
