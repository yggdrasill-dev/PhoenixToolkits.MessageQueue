using System.Threading.Tasks.Dataflow;
using Valhalla.MessageQueue;

namespace Valhalla.MessageQueue.InProcess;

internal class InProcessMessageQueue : IEventBus, IMessageSender
{
	private readonly BroadcastBlock<InProcessMessage> m_BroadcastBlock = new(null);

	public IObservable<InProcessMessage> MessageObservable => m_BroadcastBlock.AsObservable();

	public ValueTask<Answer> AskAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask PublishAsync(
		string subject,
		ReadOnlyMemory<byte> data,
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
			data.ToArray(),
			appendHeaders,
			new());

		_ = await m_BroadcastBlock.SendAsync(
			msg,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<ReadOnlyMemory<byte>> RequestAsync(
		string subject,
		ReadOnlyMemory<byte> data,
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
			data.ToArray(),
			appendHeaders,
			new());

		_ = await m_BroadcastBlock.SendAsync(
			msg,
			cancellationToken).ConfigureAwait(false);

		return await msg.CompletionSource.Task.ConfigureAwait(false);
	}

	public async ValueTask SendAsync(
		string subject,
		ReadOnlyMemory<byte> data,
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
			data.ToArray(),
			appendHeaders,
			new());

		_ = await m_BroadcastBlock.SendAsync(
			msg,
			cancellationToken).ConfigureAwait(false);

		_ = await msg.CompletionSource.Task.ConfigureAwait(false);
	}
}
