using System.Buffers;
using System.Runtime.CompilerServices;

namespace Valhalla.MessageQueue.RabbitMQ;

public class RabbitMQRawDeserializer<T> : IRabbitMQDeserializer<T>
{
	public static readonly RabbitMQRawDeserializer<T> Default = new(null);
	private readonly IRabbitMQDeserializer<T>? m_Next;

	public RabbitMQRawDeserializer(IRabbitMQDeserializer<T>? next)
	{
		m_Next = next;
	}

	public T? Deserialize(in ReadOnlySequence<byte> buffer)
	{
		if (typeof(T) == typeof(ReadOnlySequence<byte>))
			return (T)(object)buffer;

		var span = buffer.IsSingleSegment ? buffer.FirstSpan : buffer.ToArray();

		return RabbitMQRawDeserializer<T>.TryDeserialize(span, out var result)
			? result
			: m_Next == null
				? throw new RabbitMQException($"Can't deserialize {typeof(T)}")
				: m_Next.Deserialize(buffer);
	}

	private static bool TryDeserialize(in ReadOnlySpan<byte> span, out T result)
	{
		if (typeof(T) == typeof(byte[]))
		{
			var arr = span.ToArray();
			result = Unsafe.As<byte[], T>(ref arr);
			return true;
		}

		if (typeof(T) == typeof(Memory<byte>))
		{
			var memory = new Memory<byte>(span.ToArray());
			result = Unsafe.As<Memory<byte>, T>(ref memory);
			return true;
		}

		if (typeof(T) == typeof(ReadOnlyMemory<byte>))
		{
			var memory = new ReadOnlyMemory<byte>(span.ToArray());
			result = Unsafe.As<ReadOnlyMemory<byte>, T>(ref memory);
			return true;
		}

		result = default!;
		return false;
	}
}
