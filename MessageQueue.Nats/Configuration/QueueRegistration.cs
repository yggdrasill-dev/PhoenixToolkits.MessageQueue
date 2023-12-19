﻿using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private readonly QueueSessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>> m_QueueSessionRegistration;

	public string Queue { get; }

	public string Subject { get; }

	public QueueRegistration(string subject, string queue, INatsSerializerRegistry? natsSerializerRegistry)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;

		m_QueueSessionRegistration = new QueueSessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>>(
			Subject,
			Queue,
			false,
			natsSerializerRegistry);
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
