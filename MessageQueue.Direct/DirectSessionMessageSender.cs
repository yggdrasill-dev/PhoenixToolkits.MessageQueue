using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct;

internal class DirectSessionMessageSender<TMessageSession> : IMessageSender
	where TMessageSession : IMessageSession
{
	private readonly IServiceProvider m_ServiceProvider;

	public DirectSessionMessageSender(
		IServiceProvider serviceProvider)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public async ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Ask");
		using var questionActivity = DirectDiagnostics.ActivitySource.StartActivity(subject);

		_ = (questionActivity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageSession).Name));

#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		await using var scope = m_ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
		var handler = ActivatorUtilities.CreateInstance<TMessageSession>(scope.ServiceProvider);

		var question = new DirectQuestion();

		_ = Task.Run(() => handler.HandleAsync(
			question,
			cancellationToken).AsTask(), cancellationToken);

		return await question.GetAnwserAsync().ConfigureAwait(false);
	}

	public ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}
