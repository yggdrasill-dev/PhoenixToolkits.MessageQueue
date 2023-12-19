using NATS.Client.JetStream.Models;

namespace Valhalla.MessageQueue.Nats;

internal class NatsMessageQueueService : INatsMessageQueueService
{
	private readonly INatsConnectionManager m_NatsConnectionManager;

	public NatsMessageQueueService(
		INatsConnectionManager natsConnectionManager)
	{
		m_NatsConnectionManager = natsConnectionManager ?? throw new ArgumentNullException(nameof(natsConnectionManager));
	}

	public async ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default)
	{
		var js = m_NatsConnectionManager.CreateJsContext();

		_ = await js.ListStreamsAsync(cancellationToken: cancellationToken)
			.AnyAsync(stream => stream.Info.Config.Name == config.Name, cancellationToken)
			.ConfigureAwait(false)
			? await js.UpdateStreamAsync(
				config,
				cancellationToken).ConfigureAwait(false)
			: await js.CreateStreamAsync(
				config,
				cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<IDisposable> SubscribeAsync(INatsSubscribe settings, CancellationToken cancellationToken = default)
		=> settings.SubscribeAsync(m_NatsConnectionManager, cancellationToken);
}
