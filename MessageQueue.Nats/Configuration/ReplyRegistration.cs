using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class ReplyRegistration<THandler> : ISubscribeRegistration where THandler : IMessageHandler
{
	private readonly SessionRegistration<InternalHandlerSession<THandler>> m_SessionRegistration;

	public string Subject { get; }

	public ReplyRegistration(string subject)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;

		m_SessionRegistration = new SessionRegistration<InternalHandlerSession<THandler>>(subject);
	}

	public ValueTask<IDisposable?> SubscribeAsync(
		object receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_SessionRegistration.SubscribeAsync(
			receiver,
			serviceProvider,
			logger,
			cancellationToken);
}
