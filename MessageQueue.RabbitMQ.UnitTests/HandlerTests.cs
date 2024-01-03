using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.RabbitMQ;
using Valhalla.MessageQueue.RabbitMQ.Configuration;
using Xunit;

namespace MessageQueue.RabbitMQ.UnitTests;

public class HandlerTests
{
    [Fact(Timeout = 1000)]
    public async Task Registration_Handler執行()
    {
        // Arrange
        var fakeMessageReceiver = Substitute.For<IMessageReceiver<RabbitSubscriptionSettings>>();
        var serviceProvider = new ServiceCollection()
            .AddMessageQueue()
            .AddRabbitMessageQueue(config => { })
            .Services
            .BuildServiceProvider();

        var sut = new SubscribeRegistration<ReadOnlyMemory<byte>, StubMessageHandler>(
            "test",
            true,
            1,
           sp => new StubMessageHandler());

        var message = "aaa";

        fakeMessageReceiver.When(fake => fake.SubscribeAsync(Arg.Any<RabbitSubscriptionSettings>()))
            .Do(callInfo =>
            {
                var settings = callInfo.Arg<RabbitSubscriptionSettings>();
                settings.EventHandler(Substitute.For<IModel>(), new BasicDeliverEventArgs
                {
                    BasicProperties = Substitute.For<IBasicProperties>(),
                    Body = Encoding.UTF8.GetBytes(message)
                });
            });
        // Act
        _ = await sut.SubscribeAsync(
            fakeMessageReceiver,
            serviceProvider,
            NullLogger<StubMessageHandler>.Instance,
            default);

        var actual = await StubMessageHandler.GetResultAsync();

        // Assert
        Assert.Equal(message, actual);
    }
}
