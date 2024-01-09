using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Messaging;
using MongoDB.Messaging.Configuration;
using MongoDB.Messaging.Subscription;
using NSubstitute;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.MongoDB.Configuration;
using MongoDBMessage = MongoDB.Messaging.Message;
using MongoMessageQueue = MongoDB.Messaging.MessageQueue;

namespace MessageQueue.MongoDB.IntegrationTests;

public class DependencyInjectionTests
{
    [Fact]
    public void 建立MongoDBMessageQueue服務()
    {
        var services = new ServiceCollection();

        _ = services.AddMessageQueue()
            .AddMongoMessageQueue(configuration => configuration
                .DefineMessageQueue(
                    "test",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    })
                .DeclareQueue("queue")
                .AddHandler<StubMessageHandler>("test", TimeSpan.FromSeconds(1)));

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetRequiredService<MongoDBMessageQueueBuilder>();

        _ = sut.Build(serviceProvider);
    }

    [Fact]
    public void 建立Handler()
    {
        var services = new ServiceCollection();

        _ = services
            .AddLogging()
            .AddMessageQueue()
            .AddMongoMessageQueue(configuration => configuration
                .DefineMessageQueue(
                    "test",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    })
                .DeclareQueue("queue")
                .AddHandler<StubMessageHandler>("test", TimeSpan.FromSeconds(1)));

        var serviceProvider = services.BuildServiceProvider();

        var messageQueueBuilder = serviceProvider.GetRequiredService<MongoDBMessageQueueBuilder>();

        var sut = messageQueueBuilder.Build(serviceProvider);

        sut.Start();

        var actual = sut.Processors.First().Configuration.SubscriberFactory();

        Assert.NotNull(actual);
    }

    [Fact]
    public void 可以接收到MongoDB的Message()
    {
        var services = new ServiceCollection();

        _ = services
            .AddLogging()
            .AddMessageQueue()
            .AddMongoMessageQueue(configuration => configuration
                .DefineMessageQueue(
                    "test",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    })
                .DeclareQueue("queue")
                .AddHandler<StubMessageHandler>("test", TimeSpan.FromSeconds(1)));

        var serviceProvider = services.BuildServiceProvider();

        var messageQueueBuilder = serviceProvider.GetRequiredService<MongoDBMessageQueueBuilder>();

        var manager = messageQueueBuilder.Build(serviceProvider);

        manager.Start();

        var sut = manager.Processors.First().Configuration.SubscriberFactory();

        var container = Substitute.For<IQueueContainer>();

        var actual = sut.Process(
            new ProcessContext(
                new MongoDBMessage
                {
                    Data = []
                },
                container));

        Assert.Equal(MessageResult.Successful, actual);
    }

    [Fact]
    public void 可以取得MongoDBMessageQueueBuilders()
    {
        var services = new ServiceCollection();

        _ = services.AddMessageQueue()
            .AddMongoMessageQueue(configuration =>
            {
                _ = configuration.DefineMessageQueue(
                    "test1",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    });

                _ = configuration.DefineMessageQueue(
                    "test2",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    });
            });

        var sut = services.BuildServiceProvider();

        var actual = sut.GetServices<MongoDBMessageQueueBuilder>();

        Assert.Equal(2, actual.Count());
    }

    [Fact]
    public async Task 可以發送MongoDBMessage()
    {
        var services = new ServiceCollection();

        var fakeManager = Substitute.For<IQueueManager>();

        var mongoMessageQueue = new MongoMessageQueue(fakeManager);

        _ = services
            .AddLogging()
            .AddMessageQueue()
            .AddMongoMessageQueue(configuration => configuration
                .DefineMessageQueue(
                    "test",
                    sp => new MongoMessageQueueBuilderOptions
                    {
                        MessageQueue = MongoMessageQueue.Default,
                        ConnectionString = "mongodb://localhost/Messaging"
                    })
                .DeclareQueue("queue"))
            .AddMongoGlobPatternExchange("*", "test", "queue");

        var serviceProvider = services.BuildServiceProvider();

        foreach (var builder in serviceProvider.GetServices<MongoDBMessageQueueBuilder>())
            _ = builder.Build(serviceProvider);

        var sut = serviceProvider.GetRequiredService<IMessageSender>();

        await sut.PublishAsync("subject.test", new { Data = "Test" }.ToBson());
    }

    [Fact]
    public async Task 當有多種交換器時也可以正常發送MongoDB訊息()
    {
        var services = new ServiceCollection();

        var fakeManager = Substitute.For<IQueueManager>();

        var mongoMessageQueue = new MongoMessageQueue(fakeManager);

        _ = services
            .AddLogging()
            .AddMessageQueue()
            .AddMongoMessageQueue(configuration =>
            {
                _ = configuration
                    .DefineMessageQueue(
                        "test1",
                        sp => new MongoMessageQueueBuilderOptions
                        {
                            MessageQueue = MongoMessageQueue.Default,
                            ConnectionString = "mongodb://localhost/Messaging"
                        })
                    .DeclareQueue("queue1");

                _ = configuration
                    .DefineMessageQueue(
                        "test2",
                        sp => new MongoMessageQueueBuilderOptions
                        {
                            MessageQueue = MongoMessageQueue.Default,
                            ConnectionString = "mongodb://localhost/Messaging"
                        })
                    .DeclareQueue("queue2");
            })
            .AddMongoGlobPatternExchange("subject.test1", "test1", "queue1")
            .AddMongoGlobPatternExchange("subject.test2", "test2", "queue2");

        var serviceProvider = services.BuildServiceProvider();

        foreach (var builder in serviceProvider.GetServices<MongoDBMessageQueueBuilder>())
            _ = builder.Build(serviceProvider);

        var sut = serviceProvider.GetRequiredService<IMessageSender>();

        await sut.PublishAsync("subject.test1", new { Data = "Test" }.ToBson());
    }
}
