using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitMessageQueueService : IMessageQueueService
{
	private readonly IModel m_Channel;
	private readonly string m_ExchangeName;

	public RabbitMessageQueueService(string exchangeName, IModel channel)
	{
		m_Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		m_ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
	}

	public ValueTask<Answer> AskAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity("RabbitMQ Publish");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var properties = BuildBasicProperties(appendHeaders);

		cancellationToken.ThrowIfCancellationRequested();
		m_Channel.BasicPublish(m_ExchangeName, subject, properties, data);

		return ValueTask.CompletedTask;
	}

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	private IBasicProperties BuildBasicProperties(IEnumerable<MessageHeaderValue> header)
	{
		var properties = m_Channel.CreateBasicProperties();
		properties.Headers = header.ToDictionary(v => v.Name, v => (object?)v.Value);

		return properties;
	}
}
