using NATS.Client;
using Valhalla.MessageQueue.Nats;

namespace MessageQueue.Nats.UnitTests;

public class MsgHeaderTests
{
    [Fact]
    public void 從Msg檢查Header有沒有值()
    {
        var sut = new Msg
        {
            Header = new MsgHeader()
        };

        var headerValue = NatsMessageHeaderValueConsts.FailMessageHeaderValue;

        sut.Header.Add(headerValue.Name, headerValue.Value);

        Assert.True(sut.HasHeaders);
    }

    [Fact]
    public void 如果沒有設定FailHeader_檢查有無錯誤應該回傳空陣列()
    {
        var sut = new MsgHeader();

        var actual = sut.GetValues(NatsMessageHeaderValueConsts.FailMessageHeaderValue.Name);

        Assert.False(actual?.Length > 0);
    }

    [Fact]
    public void 取得錯誤會回傳所有的HeaderValues()
    {
        var sut = new MsgHeader();

        var headerValue = NatsMessageHeaderValueConsts.FailMessageHeaderValue;

        sut.Add(headerValue.Name, headerValue.Value);

        var actual = sut.GetValues(headerValue.Name);

        Assert.True(actual?.Length > 0);
    }
}
