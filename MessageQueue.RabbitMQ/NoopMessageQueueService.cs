namespace Valhalla.MessageQueue.RabbitMQ;

internal class NoopMessageQueueService : IMessageQueueService
{
	public ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
			=> ValueTask.CompletedTask;

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
		=> ValueTask.FromResult(new ReadOnlyMemory<byte>());

	public ValueTask SendAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;
}
