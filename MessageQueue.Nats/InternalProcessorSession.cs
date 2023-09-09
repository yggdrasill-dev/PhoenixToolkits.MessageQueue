﻿using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Valhalla.MessageQueue.Nats;

internal class InternalProcessorSession<TProcessor> : IMessageSession
	where TProcessor : IMessageProcessor
{
	private readonly TProcessor m_Processor;

	public InternalProcessorSession(IServiceProvider serviceProvider)
	{
		m_Processor = ActivatorUtilities.CreateInstance<TProcessor>(serviceProvider);
	}

	public async ValueTask HandleAsync(Question question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity("InternalHandlerSession");

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(TProcessor).Name));

		var result = await m_Processor
			.HandleAsync(question.Data, cancellationToken)
			.ConfigureAwait(false);

		await question
			.CompleteAsync(result, cancellationToken)
			.ConfigureAwait(false);
	}
}