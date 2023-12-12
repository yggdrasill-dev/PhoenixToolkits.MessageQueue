using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Xunit;

namespace MessageQueue.RabbitMQ.UnitTests;

public partial class DependencyInjectionTests
{
    [Fact]
    public void 註冊RabbitMQ()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .ConfigQueueOptions((options, sp) =>
                {
                    options.RabbitMQUrl = new Uri("http://rabbit-server-url");
                    options.UserName = "account";
                    options.Password = "password";
                }));
    }

    [Fact]
    public void 註冊RabbitMQ的Handler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddHandler<ReadOnlyMemory<byte>, StubMessageHandler>("queueName"));
    }

    [Fact]
    public void 用HandlerType註冊RabbitMQ的Handler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddHandler(typeof(StubMessageHandler), "queueName"));
    }
}
