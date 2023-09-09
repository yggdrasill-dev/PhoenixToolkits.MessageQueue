﻿using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class InternalHandlerSession<THandler> : IMessageSession
	where THandler : IMessageHandler
{
	private readonly THandler m_Handler;

	public InternalHandlerSession(IServiceProvider serviceProvider)
	{
		m_Handler = ActivatorUtilities.CreateInstance<THandler>(serviceProvider);
	}

	public async ValueTask HandleAsync(Question question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity("InternalHandlerSession");

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(THandler).Name));

		await m_Handler
			.HandleAsync(question.Data, cancellationToken)
			.ConfigureAwait(false);

		await question
			.CompleteAsync(Array.Empty<byte>(), cancellationToken)
			.ConfigureAwait(false);
	}
}