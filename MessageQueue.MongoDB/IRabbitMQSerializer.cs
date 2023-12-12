using System.Buffers;
using Valhalla.MessageQueue;

namespace Valhalla.MessageQueue.MongoDB;

public interface IRabbitMQSerializer<in TMessage>
{
	void Serialize(IBufferWriter<byte> bufferWriter, TMessage message);
}
