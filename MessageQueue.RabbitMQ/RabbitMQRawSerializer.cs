using System.Buffers;

namespace Valhalla.MessageQueue.RabbitMQ;

public class RabbitMQRawSerializer<T> : IRabbitMQSerializer<T>
{
	public static readonly RabbitMQRawSerializer<T> Default = new(null);

	private readonly IRabbitMQSerializer<T>? m_Next;

	public RabbitMQRawSerializer(IRabbitMQSerializer<T>? next)
	{
		m_Next = next;
	}

	public ReadOnlyMemory<byte> Serialize(T message)
		=> message switch
		{
			byte[] bytes => bytes,
			Memory<byte> memory => memory.ToArray(),
			ReadOnlyMemory<byte> readOnlyMemory => readOnlyMemory,
			ReadOnlySequence<byte> readOnlySequence => readOnlySequence.ToArray(),
			_ => m_Next == null
				? throw new RabbitMQException($"Can't serialize {typeof(T)}")
				: m_Next.Serialize(message)
		};
}
