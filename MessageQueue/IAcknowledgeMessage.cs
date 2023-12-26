namespace Valhalla.MessageQueue;

public interface IAcknowledgeMessage<TMessage>
{
	string Subject { get; }

	TMessage? Data { get; }

	IEnumerable<MessageHeaderValue>? HeaderValues { get; }

	ValueTask AckAsync(CancellationToken cancellationToken = default);

	ValueTask AckProgressAsync(CancellationToken cancellationToken = default);

	ValueTask AckTerminateAsync(CancellationToken cancellationToken = default);

	ValueTask NakAsync(TimeSpan delay = default, CancellationToken cancellationToken = default);
}
