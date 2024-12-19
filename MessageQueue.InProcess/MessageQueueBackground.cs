using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.MessageQueue.InProcess.Configuration;
using Valhalla.Messages;

namespace Valhalla.MessageQueue.InProcess;

internal class MessageQueueBackground : BackgroundService
{
	private readonly ILogger<MessageQueueBackground> m_Logger;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEventBus m_EventBus;
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
		=> m_EventBus.GetEventObserable<InProcessMessage>(EventBusNames.InProcessEventBusName)!
			.Do(_ => m_Logger.LogDebug("msg inbound"))
			.Select(msg => Observable.FromAsync(() => Task.Run(
				async () =>
				{
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
					using var activity = TraceContextPropagator.TryExtract(
						msg.MessageHeaders.ToLookup(hv => hv.Name, hv => hv.Value),
						(header, key) => header[key]?.FirstOrDefault(),
						out var context)
						? InProcessDiagnostics.ActivitySource.StartActivity(
							msg.Subject,
							ActivityKind.Internal,
							context)
						: InProcessDiagnostics.ActivitySource.StartActivity(msg.Subject);

					var scope = m_ServiceProvider.CreateAsyncScope();
					await using (scope.ConfigureAwait(false))
					{
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
								msg.MessageHeaders,
								cts.Token).ConfigureAwait(false);
						}
						else
						{
							m_Logger.LogError("Subject ({subject}) can't found handler or processor.", msg.Subject);

							msg.CompletionSource.SetException(new HandlerOrProcessorNotFoundException());
						}
					}
				},
				stoppingToken))
				.Catch((Exception ex) => Observable.FromAsync(async () =>
				{
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
					m_Logger.LogError(ex, "Handle message occur error.");

					foreach (var handler in m_ServiceProvider.GetServices<ExceptionHandler>())
						await handler.HandleExceptionAsync(
							ex,
							cts.Token).ConfigureAwait(false);
					return Unit.Default;
				})))
			.Merge()
			.ToTask(stoppingToken);
}
