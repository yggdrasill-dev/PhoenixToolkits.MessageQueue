namespace Valhalla.MessageQueue;

public static class MessageSenderExtensions
{
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
