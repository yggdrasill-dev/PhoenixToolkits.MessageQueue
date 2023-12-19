using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
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
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var sessionReplySubject = "test.reply";

        var fakeNatsConnection = Substitute.For<INatsConnection>();

        var sut = new NatsMessageSender(
            null,
            sessionReplySubject,
            fakeNatsConnection,
            fakePromiseStore,
            NullLogger<NatsMessageSender>.Instance);

        var askId = Guid.NewGuid();
        var subject = "test";

        _ = fakeNatsConnection.RequestAsync<string, string>(
            Arg.Is(subject),
            Arg.Any<string>(),
            Arg.Any<NatsHeaders>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new NatsMsg<string>
            {
                Subject = subject,
                Data = "bbb",
                Headers = new NatsHeaders
                {
                    [MessageHeaderValueConsts.SessionReplySubjectKey] = sessionReplySubject,
                    [MessageHeaderValueConsts.SessionAskKey] = askId.ToString()
                }
            });

        var actual = await sut.AskAsync<string, string>(subject, "aaa");

        Assert.NotNull(actual);
        Assert.IsType<NatsAnswer<string>>(actual);

        var answer = actual as NatsAnswer<string>;
        Assert.Equal(sessionReplySubject, answer?.ReplySubject);
        Assert.Equal(askId, answer?.ReplyId);
    }

    [Fact]
    public async Task 要呼叫InternalAsk前會先移除Header中的SessionAskKey()
    {
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var sessionReplySubject = "test.reply";
        var sut = new NatsMessageSender(
            null,
            sessionReplySubject,
            Substitute.For<INatsConnection>(),
            fakePromiseStore,
            NullLogger<NatsMessageSender>.Instance);

        var askId = Guid.NewGuid();

        var tcs = new TaskCompletionSource<Answer<string>>();
        tcs.SetResult(new NatsAnswer<string>("aaa", sut, sessionReplySubject, askId));

        _ = fakePromiseStore.CreatePromise<string>(Arg.Any<CancellationToken>())
            .Returns((askId, tcs.Task));

        var actual = await sut.AskAsync<string, string>(
            "test",
            "aaa",
            new[] { new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) },
            default);

        Assert.NotNull(actual);
        Assert.IsType<NatsAnswer<string>>(actual);

        var answer = actual as NatsAnswer<string>;
        Assert.Equal(sessionReplySubject, answer?.ReplySubject);
        Assert.Equal(askId, answer?.ReplyId);
    }

    [Fact(Timeout = 1000)]
    public async Task 處理ReplyHandler()
    {
        var registration = new SessionReplyRegistration("test", null);
        var fakeMessageReceiver = Substitute.For<IMessageReceiver<INatsSubscribe>>();
        var fakePromiseStore = Substitute.For<IReplyPromiseStore>();

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection
            .AddSingleton(fakePromiseStore)
            .AddSingleton(Substitute.For<IMessageSender>());

        var fakeServiceProvider = serviceCollection.BuildServiceProvider();

        var replyId = Guid.NewGuid();
        var askId = Guid.NewGuid();

        var msg = new NatsMsg<ReadOnlySequence<byte>>
        {
            Subject = "test",
            Data = new ReadOnlySequence<byte>([]),
            Headers = new NatsHeaders
            {
                [MessageHeaderValueConsts.SessionReplyKey] = replyId.ToString(),
                [MessageHeaderValueConsts.SessionReplySubjectKey] = "test"
            }
        };

        var tcs = new TaskCompletionSource<Answer<ReadOnlySequence<byte>>>();
        fakePromiseStore
            .When(fake => fake.SetResult(Arg.Is(replyId), Arg.Any<Answer<ReadOnlySequence<byte>>>()))
            .Do(callInfo =>
            {
                var answer = callInfo.Arg<Answer<ReadOnlySequence<byte>>>();

                tcs.SetResult(answer);
            });
        _ = fakePromiseStore.GetPromiseType(Arg.Is(replyId))
            .Returns(typeof(ReadOnlySequence<byte>));

        fakeMessageReceiver.WhenForAnyArgs(fake => fake.SubscribeAsync(Arg.Any<INatsSubscribe>()))
            .Do(callInfo =>
            {
                var settings = callInfo.Arg<INatsSubscribe>() as NatsSubscriptionSettings<ReadOnlySequence<byte>>;

                settings?.EventHandler(msg, default);
            });

        _ = await registration.SubscribeAsync(
            fakeMessageReceiver,
            fakeServiceProvider,
            NullLogger.Instance,
            default);

        var actual = await tcs.Task;
    }
}
