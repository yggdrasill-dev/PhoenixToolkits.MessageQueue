namespace Valhalla.MessageQueue;

public static class MessageSenderExtensions
{
	public static ValueTask<Answer<TReply>> AskAsync<TAnswer, TMessage, TReply>(
		this Answer<TAnswer> answer,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> answer.AskAsync<TMessage, TReply>(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask<Answer<TReply>> AskAsync<TQuestion, TMessage, TReply>(
		this Question<TQuestion> question,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> question.AskAsync<TMessage, TReply>(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		this IMessageSender messageSender,
		string subject,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> messageSender.AskAsync<TMessage, TReply>(
			subject,
			data,
			Array.Empty<MessageHeaderValue>(),
			cancellationToken);

	public static ValueTask CompleteAsync<TAnswer>(
		this Answer<TAnswer> answer,
		CancellationToken cancellationToken = default)
		=> answer.CompleteAsync(Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask CompleteAsync<TQuestion>(
		this Question<TQuestion> question,
		CancellationToken cancellationToken = default)
		=> question.CompleteAsync(Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask CompleteAsync<TAnswer, TReply>(
		this Answer<TAnswer> answer,
		TReply data,
		CancellationToken cancellationToken = default)
		=> answer.CompleteAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask CompleteAsync<TQuestion, TReply>(
		this Question<TQuestion> question,
		TReply data,
		CancellationToken cancellationToken = default)
		=> question.CompleteAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask FailAsync<TAnswer>(
		this Answer<TAnswer> answer,
		string data,
		CancellationToken cancellationToken = default)
		=> answer.FailAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask FailAsync<TQuestion>(
		this Question<TQuestion> question,
		string data,
		CancellationToken cancellationToken = default)
		=> question.FailAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask PublishAsync<TMessage>(
		this IMessageSender messageSender,
		string subject,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask PublishFailAsync(
		this IMessageSender messageSender,
		string subject,
		string data,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(
			subject,
			Array.Empty<byte>(),
			new[] { new MessageHeaderValue(MessageHeaderValueConsts.FailHeaderKey, data) },
			cancellationToken);

	public static ValueTask PublishFailAsync(
		this IMessageSender messageSender,
		string subject,
		string data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(
			subject,
			Array.Empty<byte>(),
			header.Append(new MessageHeaderValue(MessageHeaderValueConsts.FailHeaderKey, data)),
			cancellationToken);

	public static ValueTask<TReply> RequestAsync<TMessage, TReply>(
		this IMessageSender messageSender,
		string subject,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> messageSender.RequestAsync<TMessage, TReply>(
			subject,
			data,
			Array.Empty<MessageHeaderValue>(),
			cancellationToken);

	public static ValueTask SendAsync<TMessage>(
		this IMessageSender messageSender,
		string subject,
		TMessage data,
		CancellationToken cancellationToken = default)
		=> messageSender.SendAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);
}
