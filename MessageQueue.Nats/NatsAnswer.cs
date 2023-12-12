namespace Valhalla.MessageQueue.Nats;

internal record NatsAnswer<TAnswer> : Answer<TAnswer>
{
	private readonly IMessageSender m_MessageSender;

	public override bool CanResponse => !string.IsNullOrEmpty(ReplySubject);

	internal string? ReplySubject { get; }

	internal Guid? ReplyId { get; }

	internal NatsAnswer(
		TAnswer responseData,
		IMessageSender messageSender,
		string? replySubject,
		Guid? replyId)
	{
		Result = responseData;
		ReplySubject = replySubject;
		ReplyId = replyId;
		m_MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public override ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.AskAsync<TMessage, TReply>(
				ReplySubject!,
				data,
				header.Concat(new[] {
					new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString()),
					new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, string.Empty) }),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(
				ReplySubject!,
				data,
				header.Concat(new[] {
					new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString())}),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishAsync(
				ReplySubject!,
				Array.Empty<byte>(),
				header.Append(new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString())),
				cancellationToken)
			: throw new NatsReplySubjectNullException();

	public override ValueTask FailAsync(
		string data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> CanResponse
			? m_MessageSender.PublishFailAsync(
				ReplySubject!,
				data,
				header.Append(new MessageHeaderValue(MessageHeaderValueConsts.SessionReplyKey, ReplyId.ToString())),
				cancellationToken: cancellationToken)
			: throw new NatsReplySubjectNullException();
}
