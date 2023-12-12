namespace Valhalla.MessageQueue.MongoDB;

internal class MongoMessage<TData>
{
	public TData? Data { get; init; }

	public string Subject { get; init; } = default!;

	public string? TraceParent { get; internal set; }

	public string? TraceState { get; internal set; }

	public MessageHeaderValue[]? Headers { get; internal set; }
}
