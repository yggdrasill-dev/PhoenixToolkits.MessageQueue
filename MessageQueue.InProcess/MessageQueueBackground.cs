﻿using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.InProcess.Configuration;

namespace Valhalla.MessageQueue.InProcess;

internal class MessageQueueBackground : BackgroundService
{
	private readonly IEventBus m_EventBus;
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_SubscribeRegistrations;

	public MessageQueueBackground(
		IEventBus eventBus,
		IEnumerable<ISubscribeRegistration> subscribeRegistrations,
		IServiceProvider serviceProvider,
		ILogger<MessageQueueBackground> logger)
	{
		m_EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		m_SubscribeRegistrations = subscribeRegistrations ?? throw new ArgumentNullException(nameof(subscribeRegistrations));
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var subscription = m_EventBus.MessageObservable
			.Do(_ => m_Logger.LogDebug("msg inbound"))
			.Select(msg => Observable.FromAsync(() => Task.Run(
				async () =>
				{
					using var activity = TraceContextPropagator.TryExtract(
						msg.MessageHeaders.ToLookup(hv => hv.Name, hv => hv.Value),
						(header, key) => string.Join(", ", header[key]),
						out var context)
						? InProcessDiagnostics.ActivitySource.StartActivity(
							msg.Subject,
							ActivityKind.Internal,
							context)
						: InProcessDiagnostics.ActivitySource.StartActivity(msg.Subject);

#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
					await using var scope = m_ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait

					bool TryFindSubscription(string subject, out IMessageHandlerExecutor executor)
					{
						foreach (var sub in m_SubscribeRegistrations)
							if (sub.SubjectGlob.IsMatch(subject))
							{
								executor = sub.CreateExecutor(scope.ServiceProvider);
								return true;
							}

						executor = default!;
						return false;
					}

					m_Logger.LogDebug("Process msg");

					if (TryFindSubscription(msg.Subject, out var handlerExecutor))
					{
						_ = (activity?.AddTag("mq", "InProcess")
							.AddTag("handler", handlerExecutor.GetType().Name));

						await handlerExecutor.HandleAsync(
							msg,
							stoppingToken).ConfigureAwait(false);
					}
					else
					{
						m_Logger.LogError("Subject ({subject}) can't found handler or processor.", msg.Subject);

						msg.CompletionSource.SetException(new HandlerOrProcessorNotFoundException());
					}
				},
				stoppingToken))
				.Catch((Exception ex) => Observable.FromAsync(async () =>
				{
					m_Logger.LogError(ex, "Handle message occur error.");

					foreach (var handler in m_ServiceProvider.GetServices<ExceptionHandler>())
						await handler.HandleExceptionAsync(
							ex,
							stoppingToken).ConfigureAwait(false);
					return Unit.Default;
				})))
			.Merge()
			.Subscribe();

		_ = stoppingToken.Register(() => subscription.Dispose());

		return Task.CompletedTask;
	}
}