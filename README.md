# PhoenixToolkits.MessageQueue

PhoenixToolkits.MessageQueue 是為了抽象化訊息傳遞的傳送、接收處理而開發的函式庫。

## 如何使用套件
安裝 [PhoenixToolkits.Message Nuget Package](https://www.nuget.org/packages/PhoenixToolkits.MessageQueue)

### .NET Core CLI

```
dotnet add package PhoenixToolkits.MessageQueue
```

再來，安裝對應實作的套件，實作套件有Direct、InProcess、NATS、MongoDB、RabbitMQ

## Examples

Dependency Injection

```csharp
builder.Services
    .AddMessageQueue();
```

加入MessageQueue System的設定
```csharp
builder.Services
    .AddMessageQueue()
    .AddDirectMessageQueue(config => config
        .AddProcessor<ProcessorType>("subject1")
        .AddHandler<HandlerType>("subject2")
        .AddReplyHandler<HandlerType2>("subject3"))
    .AddDirectGlobPatternExchange("*");
```

發送訊息
```csharp
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
 
var request = new SendMessageType
{
    ...
};

await messageSender.PublishAsync(
    "sudject1",
    request.ToByteArray(),
    cancellationToken).ConfigureAwait(false);

// or

var responseData = await messageSender.RequestAsync(
    "sudject1",
    request.ToByteArray(),
    cancellationToken).ConfigureAwait(false);

var response = ReceiveType.Parser.ParseFrom(responseData.Span);

// or

await messageSender.SendAsync(
    "sudject1",
    request.ToByteArray(),
    cancellationToken).ConfigureAwait(false);
```

接收訊息
```csharp
internal class ProcessorType : IMessageProcessor
{
    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = SendMessageType.Parser.ParseFrom(data.Span);

        ...

        return response.ToByteArray();
    }
}

internal class HandlerType : IMessageHandler
{
    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = SendMessageType.Parser.ParseFrom(data.Span);

        ...
    }
}
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
