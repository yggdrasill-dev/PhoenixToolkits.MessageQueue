namespace Valhalla.MessageQueue.Nats;

internal record NatsQuestion<TQuestion> : Question<TQuestion>
{

	public override bool CanResponse => !string.IsNullOrEmpty(m_ReplySubject);

	public override string Subject { get; }

	private readonly IMessageSender m_MessageSender;
	private readonly string? m_ReplySubject;

	public NatsQuestion(
		string subject,
		TQuestion data,
		IMessageSender messageSender,
		string? replySubject)
	{
		Subject = subject;
		Data = data;
		m_ReplySubject = replySubject;
		m_MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public override ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.AskAsync<TMessage, TReply>(
				m_ReplySubject!,
				data,
				header.Concat(new[] { new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) }),
				cancellationToken)
			: throw new NatsReplySubjectNullException();
	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(m_ReplySubject!, Array.Empty<byte>(), header, cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(m_ReplySubject!, data, header, cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask FailAsync(
		string data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishFailAsync(m_ReplySubject!, data, header, cancellationToken: cancellationToken)
			: throw new NatsReplySubjectNullException();

}
