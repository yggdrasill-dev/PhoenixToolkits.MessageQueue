using System.Collections.Concurrent;

namespace Valhalla.MessageQueue;

internal class ReplyPromiseStore : IReplyPromiseStore
{
	private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Answer>> m_Store = new();

	public (Guid Id, Task<Answer> Promise) CreatePromise(CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<Answer>();
		var id = Guid.NewGuid();

		if (!m_Store.TryAdd(id, tcs))
			throw new ReplyNotStoreException();

		_ = cancellationToken.Register(() => SetCanceled(id));

		return (id, tcs.Task);
	}

	public void SetCanceled(Guid id)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			_ = tcs.TrySetCanceled();
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetException(Guid id, Exception ex)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			_ = tcs.TrySetException(ex);
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetResult(Guid id, Answer answer)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			_ = tcs.TrySetResult(answer);
			_ = m_Store.TryRemove(id, out _);
		}
	}
}
