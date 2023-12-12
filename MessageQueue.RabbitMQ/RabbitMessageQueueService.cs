using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitMessageQueueService : IMessageQueueService
{
	private readonly IModel m_Channel;
	private readonly IRabbitMQSerializerRegistry m_SerializerRegistry;
	private readonly string m_ExchangeName;

	public RabbitMessageQueueService(string exchangeName, IModel channel, IRabbitMQSerializerRegistry serializerRegistry)
	{
		m_Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		m_SerializerRegistry = serializerRegistry ?? throw new ArgumentNullException(nameof(serializerRegistry));
		m_ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
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

		var binaryData = m_SerializerRegistry.GetSerializer<TMessage>().Serialize(data);
		m_Channel.BasicPublish(m_ExchangeName, subject, properties, binaryData);

		return ValueTask.CompletedTask;
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
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
