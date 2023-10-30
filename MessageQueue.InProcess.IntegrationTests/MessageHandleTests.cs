using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Valhalla.MessageQueue;

namespace MessageQueue.InProcess.IntegrationTests;

public class MessageHandleTests
{
    [Fact]
    public async Task InProcess_MessageHandler處理訊息()
    {
        var host = new HostApplicationBuilder();

        host.Services
            .AddSingleton<PromiseStore>()
            .AddMessageQueue()
            .AddInProcessMessageQueue(configuration => configuration
                .AddHandler<StubMessageHandler>("test"))
            .AddInProcessGlobPatternExchange("*");

        using var app = host.Build();

        await app.StartAsync();

        using var cts = new CancellationTokenSource();

        var msgSender = app.Services.GetRequiredService<IMessageSender>();
        var promiseStore = app.Services.GetRequiredService<PromiseStore>();

        using var promiseCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        promiseCts.CancelAfter(TimeSpan.FromSeconds(1));

        var (id, promise) = promiseStore.CreatePromise(promiseCts.Token);

        await msgSender.PublishAsync("test", id.ToByteArray());

        await promise;
    }
}
