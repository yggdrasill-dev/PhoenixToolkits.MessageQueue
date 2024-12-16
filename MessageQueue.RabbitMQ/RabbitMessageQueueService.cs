using RabbitMQ.Client;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitMessageQueueService(
	string exchangeName,
	IChannel channel,
	IRabbitMQSerializerRegistry serializerRegistry)
	: IMessageQueueService
{
	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public async ValueTask PublishAsync<TMessage>(
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

		var binaryData = serializerRegistry.GetSerializer<TMessage>().Serialize(data);
		await channel.BasicPublishAsync(
			exchangeName,
			subject,
			false,
			properties,
			binaryData,
			cancellationToken).ConfigureAwait(false);
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

	private static BasicProperties BuildBasicProperties(IEnumerable<MessageHeaderValue> header)
	{
		var properties = new BasicProperties
		{
			Headers = header.ToDictionary(
				v => v.Name,
				v => (object?)v.Value)
		};

		return properties;
	}
}
