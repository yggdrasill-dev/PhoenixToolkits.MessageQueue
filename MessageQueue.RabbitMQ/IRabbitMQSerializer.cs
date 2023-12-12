using System.Buffers;

namespace Valhalla.MessageQueue.RabbitMQ;

public interface IRabbitMQSerializer<in TMessage>
{
	ReadOnlyMemory<byte> Serialize(TMessage message);
}
