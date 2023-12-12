using System.Buffers;

namespace Valhalla.MessageQueue.RabbitMQ;

public interface IRabbitMQDeserializer<out TMessage>
{
	TMessage? Deserialize(in ReadOnlySequence<byte> buffer);
}
