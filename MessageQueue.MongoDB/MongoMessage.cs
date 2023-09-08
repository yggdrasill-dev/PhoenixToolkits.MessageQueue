namespace Valhalla.MessageQueue.MongoDB;

internal class MongoMessage
{
	public byte[] Data { get; init; } = default!;

	public string Subject { get; init; } = default!;

	public string? TraceParent { get; internal set; }

	public string? TraceState { get; internal set; }
}
