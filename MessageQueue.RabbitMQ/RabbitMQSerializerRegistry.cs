namespace Valhalla.MessageQueue.RabbitMQ;

public class RabbitMQSerializerRegistry : IRabbitMQSerializerRegistry
{
	public static readonly RabbitMQSerializerRegistry Default = new();

	public IRabbitMQSerializer<TMessage> GetSerializer<TMessage>()
		=> RabbitMQDefaultSerializer<TMessage>.Serializer;

	public IRabbitMQDeserializer<TMessage> GetDeserializer<TMessage>()
		=> RabbitMQDefaultSerializer<TMessage>.Deserializer;
}
