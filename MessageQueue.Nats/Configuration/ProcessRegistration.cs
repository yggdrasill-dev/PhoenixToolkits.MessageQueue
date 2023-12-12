using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class ProcessRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	private readonly SessionRegistration<TMessage, InternalProcessorSession<TMessage, TReply, TProcessor>> m_SessionRegistration;

	public string Subject { get; }

	public ProcessRegistration(string subject)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;

		m_SessionRegistration = new SessionRegistration<TMessage, InternalProcessorSession<TMessage, TReply, TProcessor>>(subject);
	}

	public ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_SessionRegistration.SubscribeAsync(
			receiver,
			serviceProvider,
			logger,
			cancellationToken);
}
