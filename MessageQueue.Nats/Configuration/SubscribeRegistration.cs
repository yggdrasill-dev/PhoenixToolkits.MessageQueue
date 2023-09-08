using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class SubscribeRegistration<THandler> : ISubscribeRegistration where THandler : IMessageHandler
{
	private readonly SessionRegistration<InternalHandlerSession<THandler>> m_SessionRegistration;

	public string Subject { get; }

	public SubscribeRegistration(string subject)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;

		m_SessionRegistration = new SessionRegistration<InternalHandlerSession<THandler>>(subject, false);
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<NatsSubscriptionSettings> messageReceiver,
		IMessageReceiver<NatsQueueScriptionSettings> queueReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_SessionRegistration.SubscribeAsync(
			messageReceiver,
			queueReceiver,
			serviceProvider,
			logger,
			cancellationToken);
}
