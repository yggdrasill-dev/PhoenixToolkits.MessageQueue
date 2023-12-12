namespace Valhalla.MessageQueue;

internal class NoopMessageSender : IMessageSender
{
	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult<Answer<TReply>>(new NoopAnswer<TReply>(this));

	public ValueTask PublishAsync<TMessage>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(default(TReply)!);

	public ValueTask SendAsync<TMessage>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
