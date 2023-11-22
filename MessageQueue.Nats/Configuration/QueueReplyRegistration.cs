using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueReplyRegistration<THandler> : ISubscribeRegistration where THandler : IMessageHandler
{
	private readonly QueueSessionRegistration<InternalHandlerSession<THandler>> m_QueueSessionRegistration;

	public string Queue { get; }

	public string Subject { get; }

	public QueueReplyRegistration(string subject, string queue)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;

		m_QueueSessionRegistration = new QueueSessionRegistration<InternalHandlerSession<THandler>>(subject, queue);
	}

	public ValueTask<IDisposable?> SubscribeAsync(
		object receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_QueueSessionRegistration.SubscribeAsync(
			receiver,
			serviceProvider,
			logger,
			cancellationToken);
}
