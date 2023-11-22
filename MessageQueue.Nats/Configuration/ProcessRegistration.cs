using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class ProcessRegistration<TProcessor> : ISubscribeRegistration
	where TProcessor : IMessageProcessor
{
	private readonly SessionRegistration<InternalProcessorSession<TProcessor>> m_SessionRegistration;

	public string Subject { get; }

	public ProcessRegistration(string subject)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;

		m_SessionRegistration = new SessionRegistration<InternalProcessorSession<TProcessor>>(subject);
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
