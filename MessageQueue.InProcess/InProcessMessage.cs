namespace Valhalla.MessageQueue.InProcess;
internal record InProcessMessage(
	string Subject,
	ReadOnlyMemory<byte> Message,
	IEnumerable<MessageHeaderValue> MessageHeaders,
	TaskCompletionSource<ReadOnlyMemory<byte>> CompletionSource);
