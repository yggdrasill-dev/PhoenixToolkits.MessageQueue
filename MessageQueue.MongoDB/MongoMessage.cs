namespace Valhalla.MessageQueue.MongoDB;

internal class MongoMessage<TData>
{
	public TData? Data { get; init; }

	public string Subject { get; init; } = default!;

	public MessageHeaderValue[]? Headers { get; internal set; }
}
