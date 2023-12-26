namespace Valhalla.MessageQueue;

public interface IMessageProcessor<in TMessage, TReply>
{
	ValueTask<TReply> HandleAsync(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default);
}
