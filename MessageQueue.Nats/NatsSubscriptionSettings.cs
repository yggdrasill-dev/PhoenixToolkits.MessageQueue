using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats;

internal record NatsSubscriptionSettings<TMessage>(
	string Subject,
	Func<NatsMsg<TMessage>, CancellationToken, ValueTask> EventHandler,
	INatsDeserialize<TMessage>? Deserializer) : INatsSubscribe
{
	public ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_ = Task.Run(async () =>
		{
			await foreach (var msg in connectionManager.Connection.SubscribeAsync<TMessage>(
				Subject,
				serializer: Deserializer,
				cancellationToken: cancellationToken))
			{
				if (EventHandler is not null)
					await EventHandler(msg, cts.Token).ConfigureAwait(false);
			}
		}, cts.Token);

		return ValueTask.FromResult((IDisposable)cts);
	}
}
