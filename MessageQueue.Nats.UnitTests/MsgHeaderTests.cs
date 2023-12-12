using NATS.Client;
using NATS.Client.Core;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Nats;

namespace MessageQueue.Nats.UnitTests;

public class MsgHeaderTests
{
    [Fact]
    public void 從Msg檢查Header有沒有值()
    {
        var sut = new NatsMsg<string>
        {
            Headers = new NatsHeaders()
        };

        var headerValue = MessageHeaderValueConsts.FailHeaderKey;

        sut.Headers.Add(headerValue, "aaa");

        Assert.True(sut.Headers.ContainsKey(headerValue));

    }

    [Fact]
    public void 取得錯誤會回傳所有的HeaderValues()
    {
        var sut = new NatsHeaders
        {
            [MessageHeaderValueConsts.FailHeaderKey] = "aaa"
        };

        var actual = sut.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var values);

        Assert.True(actual);
        Assert.Equal("aaa", values.ToString());
    }
}
