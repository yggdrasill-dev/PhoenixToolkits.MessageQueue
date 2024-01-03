using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueProcessRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	private readonly QueueSessionRegistration<TMessage, InternalProcessorSession<TMessage, TReply, TProcessor>> m_QueueSessionRegistration;

	public string Queue { get; }

	public string Subject { get; }

	public QueueProcessRegistration(
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, TProcessor> processorFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;

		m_QueueSessionRegistration = new QueueSessionRegistration<TMessage, InternalProcessorSession<TMessage, TReply, TProcessor>>(
			subject,
			queue,
			true,
			natsSerializerRegistry,
			sp => ActivatorUtilities.CreateInstance<InternalProcessorSession<TMessage, TReply, TProcessor>>(
				sp,
				processorFactory(sp)));
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
