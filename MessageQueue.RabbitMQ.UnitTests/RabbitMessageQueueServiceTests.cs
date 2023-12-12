using System.Text;
using NSubstitute;
using RabbitMQ.Client;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.RabbitMQ;

namespace MessageQueue.RabbitMQ.UnitTests;

public class RabbitMessageQueueServiceTests
{
    [Fact]
    public async Task RabbitMessageQueueService_送出訊息()
    {
        // Arrange
        var fakeChannel = Substitute.For<IModel>();

        var sut = new RabbitMessageQueueService(
            "test",
            fakeChannel,
            RabbitMQSerializerRegistry.Default);

        var data = Encoding.UTF8.GetBytes("aaa");

        _ = fakeChannel.CreateBasicProperties()
            .Returns(Substitute.For<IBasicProperties>());

        // Act
        await sut.PublishAsync("a.b.c", data);

        // Assert
        fakeChannel.Received(1)
            .BasicPublish(
                Arg.Is("test"),
                Arg.Is("a.b.c"),
                Arg.Any<IBasicProperties>(),
                Arg.Is<ReadOnlyMemory<byte>>(x => x.ToArray().SequenceEqual(data)));
    }
}
