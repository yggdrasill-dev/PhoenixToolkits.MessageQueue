using ExpectedObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Valhalla.MessageQueue.MongoDB;

namespace MessageQueue.MongoDB.IntegrationTests;

public class BsonSerializeTests
{
    [Fact]
    public void 序列化Memory型別資料()
    {
        var data = new MongoMessage<byte[]>
        {
            Subject = "test",
            Data = new byte[] { 1, 2, 3 }
        };

        var result = data.ToBson();

        var actual = BsonSerializer.Deserialize<MongoMessage<byte[]>>(result);

        data.ToExpectedObject().ShouldMatch(actual);
    }
}
