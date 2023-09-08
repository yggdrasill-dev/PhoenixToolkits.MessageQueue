using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct;

internal class DirectProcessorMessageSender<TMessageProcessor> : IMessageSender
	where TMessageProcessor : class, IMessageProcessor
{
	private readonly IServiceProvider m_ServiceProvider;

	public DirectProcessorMessageSender(
		IServiceProvider serviceProvider)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
		=> throw new NotSupportedException();

	public async ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var requestActivity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Request");

		using var activity = DirectDiagnostics.ActivitySource.StartActivity(subject);

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageProcessor).Name));

#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		await using var scope = m_ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		var handler = ActivatorUtilities.CreateInstance<TMessageProcessor>(scope.ServiceProvider);

		return await handler.HandleAsync(
			data,
			cancellationToken).ConfigureAwait(false);
	}

	public ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();
}
