using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct;

internal class DirectSessionMessageSender<TQuestion, TMessageSession> : IMessageSender
	where TMessageSession : IMessageSession<TQuestion>
{
	private readonly IServiceProvider m_ServiceProvider;

	public DirectSessionMessageSender(
		IServiceProvider serviceProvider)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Ask");
		using var questionActivity = DirectDiagnostics.ActivitySource.StartActivity(subject);

		_ = (questionActivity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageSession).Name));

		var scope = m_ServiceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			var handler = ActivatorUtilities.CreateInstance<TMessageSession>(scope.ServiceProvider);

			var question = new DirectQuestion<TQuestion>(subject, (TQuestion)(object)data!);

			_ = Task.Run(() => handler.HandleAsync(
				question,
				cancellationToken).AsTask(), cancellationToken);

			return await question.GetAnwserAsync<TReply>().ConfigureAwait(false);
		}
	}

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}
