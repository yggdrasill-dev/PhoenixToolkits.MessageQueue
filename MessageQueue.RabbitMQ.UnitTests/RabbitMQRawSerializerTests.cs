using System.Buffers;
using System.Text;
using Valhalla.MessageQueue.RabbitMQ;

namespace MessageQueue.RabbitMQ.UnitTests;

public class RabbitMQRawSerializerTests
{
    [Fact]
    public void RabbitMQRawSerializer_序列化ByteArray()
    {
        // Arrange
        var sut = RabbitMQRawSerializer<byte[]>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Serialize(data);

        // Assert
        Assert.Equal(data, actual);
    }

    [Fact]
    public void RabbitMQRawSerializer_序列化Memory()
    {
        // Arrange
        var sut = RabbitMQRawSerializer<Memory<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Serialize(data);

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawSerializer_序列化ReadOnlyMemory()
    {
        // Arrange
        var sut = RabbitMQRawSerializer<ReadOnlyMemory<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Serialize(data);

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawSerializer_序列化ReadOnlySequence()
    {
        // Arrange
        var sut = RabbitMQRawSerializer<ReadOnlySequence<byte>>.Default;

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        var actual = sut.Serialize(new ReadOnlySequence<byte>(data));

        // Assert
        Assert.Equal(data, actual.ToArray());
    }

    [Fact]
    public void RabbitMQRawSerializer_序列化不支援的型別()
    {
        // Arrange
        var sut = RabbitMQRawSerializer<string>.Default;

        var data = "aaa";

        // Act
        _ = Assert.Throws<RabbitMQException>(() => sut.Serialize(data));
    }
}
