namespace Valhalla.MessageQueue;
internal record NoopAnswer : Answer
{
	private readonly IMessageSender m_MessageSender;

	public override bool CanResponse => true;

	public NoopAnswer(IMessageSender messageSender)
	{
		Result = Array.Empty<byte>();
		m_MessageSender = messageSender;
	}

	public override ValueTask<Answer> AskAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> m_MessageSender.AskAsync(
			"noop",
			data,
			cancellationToken);

	public override ValueTask CompleteAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public override ValueTask FailAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
