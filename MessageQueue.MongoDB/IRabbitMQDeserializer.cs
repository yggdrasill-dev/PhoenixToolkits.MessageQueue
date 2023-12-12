using System.Buffers;

namespace Valhalla.MessageQueue.MongoDB;

public interface IRabbitMQDeserializer<out TMessage>
{
	TMessage? Deserialize(in ReadOnlySequence<byte> buffer);
}
