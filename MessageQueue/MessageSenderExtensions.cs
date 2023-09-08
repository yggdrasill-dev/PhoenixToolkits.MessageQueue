namespace Valhalla.MessageQueue;

public static class MessageSenderExtensions
{
	public static ValueTask<Answer> AskAsync(
		this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> messageSender.AskAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask<Answer> AskAsync(
		this Answer answer,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> answer.AskAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask<Answer> AskAsync(
		this Question question,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> question.AskAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask CompleteAsync(
			this Answer answer,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> answer.CompleteAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask CompleteAsync(
		this Question question,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> question.CompleteAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask FailAsync(
			this Answer answer,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> answer.FailAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask FailAsync(
		this Question question,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> question.FailAsync(data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask PublishAsync(
			this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> messageSender.RequestAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);

	public static ValueTask SendAsync(
		this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> messageSender.SendAsync(subject, data, Array.Empty<MessageHeaderValue>(), cancellationToken);
}
