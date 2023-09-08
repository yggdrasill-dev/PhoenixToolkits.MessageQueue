namespace Valhalla.MessageQueue.Nats;

internal record NatsQuestion : Question
{
	private readonly string? m_ReplySubject;
	private readonly IMessageSender m_MessageSender;

	public override bool CanResponse => !string.IsNullOrEmpty(m_ReplySubject);

	public NatsQuestion(
		ReadOnlyMemory<byte> data,
		IMessageSender messageSender,
		string? replySubject)
	{
		Data = data;
		m_ReplySubject = replySubject;
		m_MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public override ValueTask<Answer> AskAsync(
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.AskAsync(
				m_ReplySubject!,
				data,
				header.Concat(new[] { new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) }),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(m_ReplySubject!, data, header, cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask FailAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishFailAsync(m_ReplySubject!, data, header, cancellationToken: cancellationToken)
			: throw new NatsReplySubjectNullException();
}
