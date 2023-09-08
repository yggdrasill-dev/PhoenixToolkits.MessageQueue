namespace Valhalla.MessageQueue.Nats;

internal record NatsAnswer : Answer
{
	private readonly IMessageSender m_MessageSender;

	public override bool CanResponse => !string.IsNullOrEmpty(ReplySubject);

	internal string? ReplySubject { get; }

	internal Guid? ReplyId { get; }

	internal NatsAnswer(
		ReadOnlyMemory<byte> responseData,
		IMessageSender messageSender,
		string? replySubject,
		Guid? replyId)
	{
		Result = responseData;
		ReplySubject = replySubject;
		ReplyId = replyId;
		m_MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public override ValueTask<Answer> AskAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.AskAsync(
				ReplySubject!,
				data,
				header.Concat(new[] {
					new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString()),
					new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) }),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(
				ReplySubject!,
				data,
				header.Concat(new[] {
					new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString())}),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask FailAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishFailAsync(
				ReplySubject!,
				data,
				header.Concat(new[] {
					new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString())}),
				cancellationToken: cancellationToken)
			: throw new NatsReplySubjectNullException();
}
