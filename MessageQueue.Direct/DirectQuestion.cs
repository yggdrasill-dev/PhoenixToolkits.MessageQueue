namespace Valhalla.MessageQueue.Direct;
internal record DirectQuestion : Question
{
	private readonly TaskCompletionSource<DirectAnswer> m_TaskCompletionSource = new();

	public override bool CanResponse => true;

	public override ValueTask CompleteAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Complete Question");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion).Name));

		var answer = new DirectAnswer(data, false);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return ValueTask.CompletedTask;
	}

	public override ValueTask FailAsync(ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Fail");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion).Name));

		var answer = new DirectAnswer(data, false);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return ValueTask.CompletedTask;
	}

	public override async ValueTask<Answer> AskAsync(
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Ask Question");

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(DirectQuestion).Name));

		var answer = new DirectAnswer(data, true);

		_ = m_TaskCompletionSource.TrySetResult(answer);

		return await answer.GetAnwserAsync().ConfigureAwait(false);
	}

	internal async ValueTask<DirectAnswer> GetAnwserAsync()
		=> await m_TaskCompletionSource.Task.ConfigureAwait(false);
}
