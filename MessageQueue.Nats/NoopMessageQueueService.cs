using NATS.Client.JetStream.Models;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class NoopMessageQueueService : INatsMessageQueueService
{
	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	public IEnumerable<IMessageExchange> BuildJetStreamExchanges(
		IEnumerable<JetStreamExchangeRegistration> registrations,
		IServiceProvider serviceProvider)
		=> Array.Empty<IMessageExchange>();

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.FromResult(default(TReply)!);

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;

	public ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask<IDisposable> SubscribeAsync(INatsSubscribe settings, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult<IDisposable>(null!);
}
