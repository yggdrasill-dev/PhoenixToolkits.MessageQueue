using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client;
using NSubstitute;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Nats;
using Valhalla.MessageQueue.Nats.Configuration;

namespace MessageQueue.Nats.UnitTests;

public class AskModeTests
{
    [Fact]
    public async Task Ask應該回傳Answer()
    {
        var fakeNatsConnection = Substitute.For<IConnection>();
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var sessionReplySubject = "test.reply";
        var sut = new NatsMessageQueueService(
            fakeNatsConnection,
            sessionReplySubject,
            fakePromiseStore,
            NullLogger<NatsMessageQueueService>.Instance);

        var askId = Guid.NewGuid();

        _ = fakeNatsConnection.RequestAsync(Arg.Any<Msg>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var msg = callInfo.Arg<Msg>();

                var header = new MsgHeader
                {
                    {
                        MessageHeaderValueConsts.SessionReplySubjectKey,
                        msg.Header.GetValues(MessageHeaderValueConsts.SessionReplySubjectKey).First()
                    },
                    {
                        MessageHeaderValueConsts.SessionAskKey,
                        askId.ToString()
                    }
                };

                return new Msg(msg.Subject, "reply", header, Array.Empty<byte>());
            });

        var actual = await sut.AskAsync("test", Array.Empty<byte>());

        Assert.NotNull(actual);
        Assert.IsType<NatsAnswer>(actual);

        Assert.Equal(sessionReplySubject, (actual as NatsAnswer)?.ReplySubject);
        Assert.Equal(askId, (actual as NatsAnswer)?.ReplyId);
    }

    [Fact]
    public async Task InternalAsk應該回傳Answer()
    {
        var fakeNatsConnection = Substitute.For<IConnection>();
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var sessionReplySubject = "test.reply";
        var sut = new NatsMessageQueueService(
            fakeNatsConnection,
            sessionReplySubject,
            fakePromiseStore,
            NullLogger<NatsMessageQueueService>.Instance);

        var askId = Guid.NewGuid();

        var tcs = new TaskCompletionSource<Answer>();
        tcs.SetResult(new NatsAnswer(Array.Empty<byte>(), sut, sessionReplySubject, askId));

        _ = fakePromiseStore.CreatePromise(Arg.Any<CancellationToken>())
            .Returns((askId, tcs.Task));

        var actual = await sut.AskAsync(
            "test",
            Array.Empty<byte>(),
            new[] { new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) },
            default);

        Assert.NotNull(actual);
        Assert.IsType<NatsAnswer>(actual);

        Assert.Equal(sessionReplySubject, (actual as NatsAnswer)?.ReplySubject);
        Assert.Equal(askId, (actual as NatsAnswer)?.ReplyId);
    }

    [Fact]
    public async Task 處理ReplyHandler()
    {
        var registration = new SessionReplyRegistration("test");
        var fakeMessageReceiver = Substitute.For<IMessageReceiver<NatsSubscriptionSettings>>();
        var fakeQueueReceiver = Substitute.For<IMessageReceiver<NatsQueueScriptionSettings>>();
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection
            .AddSingleton(fakePromiseStore)
            .AddSingleton(Substitute.For<IMessageSender>());

        var fakeServiceProvider = serviceCollection.BuildServiceProvider();

        EventHandler<MsgHandlerEventArgs>? sut = null;

        fakeMessageReceiver
            .When(fake => fake.SubscribeAsync(Arg.Any<NatsSubscriptionSettings>()))
            .Do(callInfo =>
            {
                var setting = callInfo.Arg<NatsSubscriptionSettings>();

                sut = setting.EventHandler;
            });

        _ = await registration.SubscribeAsync(
            fakeMessageReceiver,
            fakeQueueReceiver,
            fakeServiceProvider,
            NullLogger.Instance,
            default);

        var replyId = Guid.NewGuid();
        var askId = Guid.NewGuid();

        var eventArg = new MsgHandlerEventArgs(new Msg("test", new MsgHeader {
            { MessageHeaderValueConsts.SessionReplyKey, replyId.ToString() },
            { MessageHeaderValueConsts.SessionReplySubjectKey, "test"}
        }, Array.Empty<byte>()));

        var tcs = new TaskCompletionSource<Answer>();
        fakePromiseStore
            .When(fake => fake.SetResult(Arg.Is(replyId), Arg.Any<Answer>()))
            .Do(callInfo =>
            {
                var answer = callInfo.Arg<Answer>();

                tcs.SetResult(answer);
            });

        sut?.Invoke(null, eventArg);

        var actual = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }
}
