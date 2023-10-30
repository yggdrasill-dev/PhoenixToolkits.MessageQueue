using System.Collections.Concurrent;
using Valhalla.MessageQueue;

namespace MessageQueue.InProcess.IntegrationTests;

internal class PromiseStore
{
	private readonly ConcurrentDictionary<Guid, TaskCompletionSource> m_Store = new();

	public (Guid Id, Task Promise) CreatePromise(CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource();
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

	public void SetResult(Guid id)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			_ = tcs.TrySetResult();
			_ = m_Store.TryRemove(id, out _);
		}
	}
}
