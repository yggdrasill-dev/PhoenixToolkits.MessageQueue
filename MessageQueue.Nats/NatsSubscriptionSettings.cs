using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats;

internal record NatsSubscriptionSettings<TMessage>(
	string Subject,
	Func<NatsMsg<TMessage>, CancellationToken, ValueTask> EventHandler,
	INatsDeserialize<TMessage>? Deserializer) : INatsSubscribe
{
	public ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var ctd = new CancellationTokenDisposable(cancellationToken);

		async void Core(CancellationToken token)
		{
			await foreach (var msg in connectionManager.Connection.SubscribeAsync<TMessage>(
				Subject,
				serializer: Deserializer,
				cancellationToken: token))
			{
				if (EventHandler is not null)
					await EventHandler(msg, token).ConfigureAwait(false);
			}
		}

		Core(ctd.Token);

		return ValueTask.FromResult((IDisposable)ctd);
	}
}
