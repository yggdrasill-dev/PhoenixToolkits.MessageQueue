using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Valhalla.MessageQueue.Nats;

record JetStreamSubscriptionSettings<TMessage>(
	string Subject,
	string Stream,
	ConsumerConfig ConsumerConfig,
	Func<NatsJSMsg<TMessage>, CancellationToken, ValueTask> EventHandler)
	: INatsSubscribe
{
	public async ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var js = connectionManager.CreateJsContext();

		var consumer = await js.CreateOrUpdateConsumerAsync(
			Stream,
			ConsumerConfig,
			cancellationToken).ConfigureAwait(false);

		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_ = Task.Run(async () =>
		{
			await foreach (var msg in consumer.ConsumeAsync<TMessage>(cancellationToken: cts.Token))
			{
				if (EventHandler is not null)
					await EventHandler(msg, cts.Token).ConfigureAwait(false);
			}
		}, cts.Token);

		return cts;
	}
}
