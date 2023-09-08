using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Direct;

internal class DirectHandlerMessageSender<TMessageHandler> : IMessageSender
	where TMessageHandler : class, IMessageHandler
{
	private readonly ILogger<DirectHandlerMessageSender<TMessageHandler>> m_Logger;
	private readonly IServiceProvider m_ServiceProvider;

	public DirectHandlerMessageSender(
		IServiceProvider serviceProvider,
		ILogger<DirectHandlerMessageSender<TMessageHandler>> logger)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<Answer> AskAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Publish");

		_ = HandleMessageAsync(
			subject,
			data,
			cancellationToken).AsTask()
			.ContinueWith(
				async t =>
				{
					if (t.IsFaulted)
					{
						var ex = t.Exception!;

						m_Logger.LogError(ex, "Handle {subject} occur error.", subject);

						foreach (var handler in m_ServiceProvider.GetServices<ExceptionHandler>())
							await handler.HandleExceptionAsync(
								ex,
								cancellationToken).ConfigureAwait(false);
					}
				},
				cancellationToken,
				TaskContinuationOptions.None,
				TaskScheduler.Current);

		return ValueTask.CompletedTask;
	}

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Publish");

		await HandleMessageAsync(
			subject,
			data,
			cancellationToken).ConfigureAwait(false);
	}

	private async ValueTask HandleMessageAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity(subject);

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageHandler).Name));

#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		await using var scope = m_ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		var handler = ActivatorUtilities.CreateInstance<TMessageHandler>(scope.ServiceProvider);

		await handler.HandleAsync(data, cancellationToken).ConfigureAwait(false);
	}
}
