using Valhalla.MessageQueue.MongoDB.Configuration;
using MongoMessageQueue = MongoDB.Messaging.MessageQueue;

namespace Valhalla.MessageQueue.MongoDB;

internal class MongoDbMessageQueueService : IMessageSender
{
	private readonly MongoMessageQueue m_MongoMessageQueue;
	private readonly string m_QueueName;

	public MongoDbMessageQueueService(MongoMessageQueue mongoMessageQueue, string queueName)
	{
		m_MongoMessageQueue = mongoMessageQueue ?? throw new ArgumentNullException(nameof(mongoMessageQueue));
		m_QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
	}

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
		using var activity = MongoDBMessageQueueConfiguration._MongoActivitySource.StartActivity("Mongo Publish");

		cancellationToken.ThrowIfCancellationRequested();

		var mongoMsg = new MongoMessage<TMessage>
		{
			Subject = subject,
			Data = data,
			Headers = header.ToArray()
		};

		TraceContextPropagator.Inject(
			activity,
			mongoMsg,
			(msg, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					switch (key)
					{
						case TraceContextPropagator.TraceParent:
							msg.TraceParent = value;
							break;

						case TraceContextPropagator.TraceState:
							msg.TraceState = value;
							break;
					}
			});

		var message = await m_MongoMessageQueue.Publish(m => m
			.Queue(m_QueueName)
			.Data(mongoMsg)
			.Correlation(Guid.NewGuid().ToString())).ConfigureAwait(false);
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
}
