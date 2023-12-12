using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct;

internal class DirectProcessorMessageSender<TData, TResult, TMessageProcessor> : IMessageSender
	where TMessageProcessor : class, IMessageProcessor<TData, TResult>
{
	private readonly IServiceProvider m_ServiceProvider;

	public DirectProcessorMessageSender(
		IServiceProvider serviceProvider)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var requestActivity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Request");

		using var activity = DirectDiagnostics.ActivitySource.StartActivity(subject);

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageProcessor).Name));

		var scope = m_ServiceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			var handler = ActivatorUtilities.CreateInstance<TMessageProcessor>(scope.ServiceProvider);

			if (data is TData messageData)
			{
				var result = await handler.HandleAsync(
					messageData,
					cancellationToken).ConfigureAwait(false);

				return (TReply)(object)result!;
			}
			else
			{
				throw new InvalidCastException($"type {typeof(TMessage).Name} Can't cast type {typeof(TData).Name}");
			}
		}
	}

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}
