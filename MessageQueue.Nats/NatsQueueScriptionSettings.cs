using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats;

internal record NatsQueueScriptionSettings<TMessage> : INatsSubscribe
{
	public string Subject { get; set; } = default!;

	public string Queue { get; set; } = default!;

	public Func<NatsMsg<TMessage>, CancellationToken, ValueTask> EventHandler { get; set; } = default!;

	public ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_ = Task.Run(async () =>
		{
			await foreach (var msg in connectionManager.Connection.SubscribeAsync<TMessage>(
				Subject,
				Queue,
				cancellationToken: cancellationToken))
			{
				if (EventHandler is not null)
					await EventHandler(msg, cts.Token).ConfigureAwait(false);
			}
		}, cts.Token);

		return ValueTask.FromResult((IDisposable)cts);
	}
}
