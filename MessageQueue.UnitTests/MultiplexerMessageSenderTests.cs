using NSubstitute;
using Valhalla.MessageQueue;

namespace MessageQueue.UnitTests;

public class MultiplexerMessageSenderTests
{
    [Fact]
    public async Task 訊息交換器會選擇第一個符合的Sender()
    {
        var fakeServiceProvider = Substitute.For<IServiceProvider>();

        var exchange1 = Substitute.For<IMessageExchange>();
        var exchange2 = Substitute.For<IMessageExchange>();

        _ = exchange1
            .Match(Arg.Any<string>(), Arg.Any<IEnumerable<MessageHeaderValue>>())
            .Returns(true);
        _ = exchange2
            .Match(Arg.Any<string>(), Arg.Any<IEnumerable<MessageHeaderValue>>())
            .Returns(true);

        var sut = new MultiplexerMessageSender(
            fakeServiceProvider,
            new[] {
                    exchange1,
                    exchange2
            });

        await sut.PublishAsync("test", Array.Empty<byte>());

        _ = exchange1.Received(1)
            .GetMessageSender(Arg.Is("test"), Arg.Any<IServiceProvider>());

        _ = exchange2.Received(0)
            .GetMessageSender(Arg.Is("test"), Arg.Any<IServiceProvider>());
    }

    [Fact]
    public void 沒有找到任何符合的訊息交換器會發生Exception()
    {
        var fakeServiceProvider = Substitute.For<IServiceProvider>();

        var sut = new MultiplexerMessageSender(
            fakeServiceProvider,
            Array.Empty<IMessageExchange>());

        _ = Assert.ThrowsAsync<MessageSenderNotFoundException>(
            async () => await sut.PublishAsync("test", Array.Empty<byte>()));
    }
}
