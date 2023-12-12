namespace Valhalla.MessageQueue.Direct;
public record DirectAnswer<TAnswer> : Answer<TAnswer>
{
	private readonly TaskCompletionSource<object> m_TaskCompletionSource = new();
	private readonly bool m_CanResponse;

	public DirectAnswer(TAnswer data, bool canResponse)
	{
		Result = data;
		m_CanResponse = canResponse;
	}
	public override bool CanResponse => m_CanResponse;

	public override async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		if (!CanResponse)
			throw new MessageCannotResponseException();

		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Answer Ask");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectAnswer<TAnswer>).Name));

		var answer = new DirectAnswer<TMessage>(data, true);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return await answer.GetAnwserAsync<TReply>().ConfigureAwait(false);
	}

	public override ValueTask CompleteAsync<TReply>(
		TReply data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		if (!CanResponse)
			throw new MessageCannotResponseException();

		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Complete Answer");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectAnswer<TAnswer>).Name));

		var answer = new DirectAnswer<TReply>(data, false);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return ValueTask.CompletedTask;
	}

	public override ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		if (!CanResponse)
			throw new MessageCannotResponseException();

		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Complete Answer");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectAnswer<TAnswer>).Name));

		_ = m_TaskCompletionSource.TrySetResult(null!);

		return ValueTask.CompletedTask;
	}

	public override ValueTask FailAsync(
		string? data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		if (!CanResponse)
			throw new MessageCannotResponseException();

		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Fail");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectAnswer<TAnswer>).Name));

		_ = m_TaskCompletionSource.TrySetException(new MessageProcessFailException(data));

		return ValueTask.CompletedTask;
	}

	internal async ValueTask<DirectAnswer<TReply>> GetAnwserAsync<TReply>()
		=> (DirectAnswer<TReply>)await m_TaskCompletionSource.Task.ConfigureAwait(false);

}
