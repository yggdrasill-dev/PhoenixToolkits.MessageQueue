using System.Buffers;
using System.Text;
using Valhalla.MessageQueue.RabbitMQ;

namespace MessageQueue.RabbitMQ.UnitTests;

public class RabbitMQRawDeserializerTests
{
    [Fact]
    public void RabbitMQRawDeserializer_反序列化ReadOnlySequence()
    {
        // Arrange
        var sut = RabbitMQRawDeserializer<ReadOnlySequence<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Deserialize(new ReadOnlySequence<byte>(data));

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawDeserializer_反序列化ByteArray()
    {
        // Arrange
        var sut = RabbitMQRawDeserializer<byte[]>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Deserialize(new ReadOnlySequence<byte>(data));

        // Assert
        Assert.Equal(data, actual);
    }

    [Fact]
    public void RabbitMQRawDeserializer_反序列化Memory()
    {
        // Arrange
        var sut = RabbitMQRawDeserializer<Memory<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Deserialize(new ReadOnlySequence<byte>(data));

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawDeserializer_反序列化ReadOnlyMemory()
    {
        // Arrange
        var sut = RabbitMQRawDeserializer<ReadOnlyMemory<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Deserialize(new ReadOnlySequence<byte>(data));

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawDeserializer_反序列化不支援的型別()
    {
        // Arrange
        var sut = RabbitMQRawDeserializer<string>.Default;

        var data = "aaa";

        // Act
        _ = Assert.Throws<RabbitMQException>(
            () => sut.Deserialize(new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(data))));
    }
}
