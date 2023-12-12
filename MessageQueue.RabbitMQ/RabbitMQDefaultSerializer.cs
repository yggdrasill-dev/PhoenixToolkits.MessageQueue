namespace Valhalla.MessageQueue.RabbitMQ;

internal static class RabbitMQDefaultSerializer<T>
{
	public static readonly IRabbitMQSerializer<T> Serializer = RabbitMQRawSerializer<T>.Default;
	public static readonly IRabbitMQDeserializer<T> Deserializer = RabbitMQRawDeserializer<T>.Default;
}
