using System.Diagnostics;
using Valhalla.Messages;

namespace Valhalla.MessageQueue.InProcess;

internal class InProcessMessageQueue : IMessageSender
{
	private readonly IEventPublisher<InProcessMessage> m_Publisher;

	public InProcessMessageQueue(IEventBus eventBus)
	{
		ArgumentNullException.ThrowIfNull(eventBus, nameof(eventBus));

		m_Publisher = eventBus.GetEventPublisher<InProcessMessage>(EventBusNames.InProcessEventBusName)
			?? throw new KeyNotFoundException($"{EventBusNames.InProcessEventBusName} event publisher not found");
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = InProcessDiagnostics.ActivitySource.StartActivity($"InProcess Publish");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var msg = new InProcessMessage(
			subject,
			data,
			appendHeaders,
			new());

		_ = await m_Publisher.PublishAsync(
			msg,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = InProcessDiagnostics.ActivitySource.StartActivity($"InProcess Request");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var msg = new InProcessMessage(
			subject,
			data,
			appendHeaders,
			new());

		_ = await m_Publisher.PublishAsync(
			msg,
			cancellationToken).ConfigureAwait(false);

		var result = await msg.CompletionSource.Task.ConfigureAwait(false);
		return (TReply)result!;
	}

	public async ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = InProcessDiagnostics.ActivitySource.StartActivity($"InProcess Send");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var msg = new InProcessMessage(
			subject,
			data,
			appendHeaders,
			new());

		_ = await m_Publisher.PublishAsync(
			msg,
			cancellationToken).ConfigureAwait(false);

		_ = await msg.CompletionSource.Task.ConfigureAwait(false);
	}
}
