namespace Valhalla.MessageQueue;

public abstract record Question<TQuestion>
{
	public abstract bool CanResponse { get; }
	public TQuestion Data { get; protected set; } = default!;
	public abstract ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default);
	public abstract ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);
	public abstract ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);
	public abstract ValueTask FailAsync(string data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);
}
