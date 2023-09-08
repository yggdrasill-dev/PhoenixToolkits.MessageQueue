namespace Valhalla.MessageQueue.Nats;

internal record NatsAction : Question
{
	public override bool CanResponse => true;

	public NatsAction(ReadOnlyMemory<byte> data)
	{
		Data = data;
	}

	public override ValueTask<Answer> AskAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public override ValueTask FailAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
