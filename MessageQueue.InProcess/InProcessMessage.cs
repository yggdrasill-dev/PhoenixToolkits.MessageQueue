namespace Valhalla.MessageQueue.InProcess;
internal record InProcessMessage(
	string Subject,
	object? Message,
	IEnumerable<MessageHeaderValue> MessageHeaders,
	TaskCompletionSource<object?> CompletionSource);
