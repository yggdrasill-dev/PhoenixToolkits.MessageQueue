namespace Valhalla.MessageQueue;
internal record NoopAnswer<TAnswer> : Answer<TAnswer>
{
	private readonly IMessageSender m_MessageSender;

	public override bool CanResponse => true;

	public NoopAnswer(IMessageSender messageSender)
	{
		Result = default!;
		m_MessageSender = messageSender;
	}

	public override ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> m_MessageSender.AskAsync<TMessage, TReply>(
			"noop",
			data,
			header,
			cancellationToken);

	public override ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public override ValueTask FailAsync(string? data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
