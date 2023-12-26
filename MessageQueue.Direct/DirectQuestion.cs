namespace Valhalla.MessageQueue.Direct;
internal record DirectQuestion<TQuestion> : Question<TQuestion>
{
	private readonly TaskCompletionSource<object> m_TaskCompletionSource = new();

	public override bool CanResponse => true;

	public DirectQuestion(
		string subject,
		TQuestion data,
		IEnumerable<MessageHeaderValue>? headerValues)
	{
		Subject = subject;
		Data = data;
		HeaderValues = headerValues;
	}

	public override async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Ask Question");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion<TQuestion>).Name));

		var answer = new DirectAnswer<TMessage>(data, true);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return await answer.GetAnwserAsync<TReply>().ConfigureAwait(false);
	}

	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Complete Question");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion<TQuestion>).Name));

		_ = m_TaskCompletionSource.TrySetResult(null!);

		return ValueTask.CompletedTask;
	}

	public override ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Complete Question");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion<TQuestion>).Name));

		var answer = new DirectAnswer<TReply>(data, false);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return ValueTask.CompletedTask;
	}

	public override ValueTask FailAsync(string? data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Fail");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion<TQuestion>).Name));

		_ = m_TaskCompletionSource.TrySetException(new MessageProcessFailException(data));

		return ValueTask.CompletedTask;
	}

	internal async ValueTask<DirectAnswer<TReply>> GetAnwserAsync<TReply>()
		=> (DirectAnswer<TReply>)await m_TaskCompletionSource.Task.ConfigureAwait(false);
}
