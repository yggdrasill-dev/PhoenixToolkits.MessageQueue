using System.Collections.Concurrent;

namespace Valhalla.MessageQueue;

internal class ReplyPromiseStore : IReplyPromiseStore
{
	private readonly ConcurrentDictionary<Guid, IPromise> m_Store = new();

	public (Guid Id, Task<Answer<TReply>> Promise) CreatePromise<TReply>(CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<Answer<TReply>>();
		var id = Guid.NewGuid();

		if (!m_Store.TryAdd(id, new Promise<TReply>(tcs)))
			throw new ReplyNotStoreException();

		_ = cancellationToken.Register(() => SetCanceled(id));

		return (id, tcs.Task);
	}

	public void SetCanceled(Guid id)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			tcs.Cancel();
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetException(Guid id, Exception ex)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			tcs.ThrowException(ex);
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetResult<TReply>(Guid id, Answer<TReply> answer)
	{
		if (m_Store.TryGetValue(id, out var tcs))
		{
			tcs.SetResult(answer);
			_ = m_Store.TryRemove(id, out _);
		}
	}
}
