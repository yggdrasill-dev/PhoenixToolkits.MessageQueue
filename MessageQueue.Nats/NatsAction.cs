namespace Valhalla.MessageQueue.Nats;

internal record NatsAction<TQuestion> : Question<TQuestion>
{
	public override bool CanResponse => false;

	public NatsAction(TQuestion data)
	{
		Data = data;
	}

	public override ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public override ValueTask FailAsync(string data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
