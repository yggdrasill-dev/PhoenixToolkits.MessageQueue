namespace Valhalla.MessageQueue.Nats;

public static class NatsMessageSenderExtensions
{
	public static ValueTask PublishFailAsync(
		this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(
			subject,
			data,
			new[] { NatsMessageHeaderValueConsts.FailMessageHeaderValue },
			cancellationToken);

	public static ValueTask PublishFailAsync(
		this IMessageSender messageSender,
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> messageSender.PublishAsync(
			subject,
			data,
			header.Concat(new[] { NatsMessageHeaderValueConsts.FailMessageHeaderValue }),
			cancellationToken);
}
