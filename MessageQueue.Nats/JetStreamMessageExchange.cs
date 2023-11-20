using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class JetStreamMessageExchange : IMessageSender
{
	private readonly IMessageExchange[] m_Exchanges;
	private readonly IServiceProvider m_ServiceProvider;

	public JetStreamMessageExchange(
		INatsMessageQueueService natsMessageQueueService,
		IEnumerable<JetStreamExchangeRegistration> exchangeRegistrations,
		IServiceProvider serviceProvider)
	{
		m_Exchanges = natsMessageQueueService.BuildJetStreamExchanges(
			exchangeRegistrations,
			serviceProvider).ToArray();
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public ValueTask<Answer> AskAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"{nameof(JetStreamMessageExchange)}.{nameof(PublishAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = GetMessageSender(subject, header);

		cancellationToken.ThrowIfCancellationRequested();

		return sender.PublishAsync(subject, data, header, cancellationToken);
	}

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	private IMessageSender GetMessageSender(string subject, IEnumerable<MessageHeaderValue> header)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity($"{nameof(JetStreamMessageExchange)}.{nameof(GetMessageSender)}");
		_ = (activity?.AddTag("subject", subject));

		foreach (var exchange in m_Exchanges)
			if (exchange.Match(subject, header))
				return exchange.GetMessageSender(subject, m_ServiceProvider);

		throw new MessageSenderNotFoundException(subject);
	}
}
