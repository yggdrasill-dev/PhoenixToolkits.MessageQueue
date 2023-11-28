using NATS.Client.JetStream;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class NoopMessageQueueService : INatsMessageQueueService
{
	public ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	public IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider)
		=> Array.Empty<IMessageExchange>();

	public ValueTask PublishAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
			=> ValueTask.CompletedTask;

	public void RegisterStream(Action<StreamConfiguration.StreamConfigurationBuilder> streamConfigure)
	{
	}

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.FromResult(new ReadOnlyMemory<byte>());

	public ValueTask SendAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;

	public ValueTask<IDisposable> SubscribeAsync(NatsQueueScriptionSettings settings) => ValueTask.FromResult<IDisposable>(null!);

	public ValueTask<IDisposable> SubscribeAsync(NatsSubscriptionSettings settings) => ValueTask.FromResult<IDisposable>(null!);

	public ValueTask<IDisposable> SubscribeAsync(JetStreamPushSubscriptionSettings settings) => ValueTask.FromResult<IDisposable>(null!);
}
