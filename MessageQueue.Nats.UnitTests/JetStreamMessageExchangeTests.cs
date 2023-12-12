using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Nats;
using Valhalla.MessageQueue.Nats.Configuration;
using Xunit;

namespace MessageQueue.Nats.UnitTests;

public class JetStreamMessageExchangeTests
{
    [Fact]
    public async Task JetSream交換器會依照Glob取得第一個符合的MessageSender()
    {
        // Arrange
        var fakeMessageQueueService = Substitute.For<INatsMessageQueueService>();
        var registrations = new[]
        {
            new JetStreamExchangeRegistration("test1"),
            new JetStreamExchangeRegistration("test2")
        };
        var fakeServiceProvider = Substitute.For<IServiceProvider>();

        var sender1 = Substitute.For<IMessageExchange>();
        var sender2 = Substitute.For<IMessageExchange>();

        _ = fakeMessageQueueService.BuildJetStreamExchanges(
            Arg.Any<IEnumerable<JetStreamExchangeRegistration>>(),
            Arg.Any<IServiceProvider>())
            .Returns(new[] { sender1, sender2 });

        var sut = new JetStreamMessageExchange(
            fakeMessageQueueService,
            registrations,
            fakeServiceProvider);

        var jetSender = Substitute.For<IMessageSender>();

        _ = sender2.Match(
            Arg.Is("test2"),
            Arg.Any<IEnumerable<MessageHeaderValue>>())
            .Returns(true);

        _ = sender2.GetMessageSender(Arg.Is("test2"), fakeServiceProvider)
            .Returns(jetSender);

        // Act
        await sut.PublishAsync(
            "test2",
            Array.Empty<byte>(),
            Array.Empty<MessageHeaderValue>());

        // Assert
        _ = jetSender.Received(1)
            .PublishAsync(Arg.Is("test2"), Arg.Any<byte[]>());
    }
}
