using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueReplyRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private readonly QueueSessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>> m_QueueSessionRegistration;

	public string Queue { get; }

	public string Subject { get; }

	public QueueReplyRegistration(
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;

		m_QueueSessionRegistration = new QueueSessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>>(
			subject,
			queue,
			true,
			natsSerializerRegistry,
			sp => ActivatorUtilities.CreateInstance<InternalHandlerSession<TMessage, THandler>>(
				sp,
				handlerFactory(sp)));
	}

	public ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_QueueSessionRegistration.SubscribeAsync(
			receiver,
			serviceProvider,
			logger,
			cancellationToken);
}
