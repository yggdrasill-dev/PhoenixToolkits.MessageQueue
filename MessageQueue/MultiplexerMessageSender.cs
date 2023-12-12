using System.Diagnostics;

namespace Valhalla.MessageQueue;

internal class MultiplexerMessageSender : IMessageSender, IMessageExchange
{
	private static readonly ActivitySource _SenderActivitySource = new($"Valhalla.MessageQueue.{nameof(MultiplexerMessageSender)}");

	private readonly IMessageExchange[] m_Exchanges = Array.Empty<IMessageExchange>();
	private readonly IServiceProvider m_ServiceProvider;

	public MultiplexerMessageSender(IServiceProvider serviceProvider, IEnumerable<IMessageExchange> exchanges)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Exchanges = (exchanges ?? throw new ArgumentNullException(nameof(exchanges))).ToArray();
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(AskAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = GetMessageSender(subject, header);

		cancellationToken.ThrowIfCancellationRequested();

		return sender.AskAsync<TMessage, TReply>(subject, data, header, cancellationToken);
	}

	public IMessageSender GetMessageSender(string pattern, IServiceProvider serviceProvider) => this;

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header) => true;

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(PublishAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = GetMessageSender(subject, header);

		cancellationToken.ThrowIfCancellationRequested();

		return sender.PublishAsync(subject, data, header, cancellationToken);
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(RequestAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = GetMessageSender(subject, header);

		cancellationToken.ThrowIfCancellationRequested();

		return sender.RequestAsync<TMessage, TReply>(subject, data, header, cancellationToken);
	}

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(SendAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = GetMessageSender(subject, header);

		cancellationToken.ThrowIfCancellationRequested();

		return sender.SendAsync(subject, data, header, cancellationToken);
	}

	private IMessageSender GetMessageSender(string subject, IEnumerable<MessageHeaderValue> header)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(GetMessageSender)}");
		_ = (activity?.AddTag("subject", subject));

		foreach (var exchange in m_Exchanges)
			if (exchange.Match(subject, header))
				return exchange.GetMessageSender(subject, m_ServiceProvider);

		throw new MessageSenderNotFoundException(subject);
	}
}
