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
		if (m_Store.TryGetValue(id, out var p))
		{
			p.Cancel();
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetException(Guid id, Exception ex)
	{
		if (m_Store.TryGetValue(id, out var p))
		{
			p.ThrowException(ex);
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public void SetResult<TReply>(Guid id, Answer<TReply> answer)
	{
		if (m_Store.TryGetValue(id, out var p))
		{
			p.SetResult(answer);
			_ = m_Store.TryRemove(id, out _);
		}
	}

	public Type? GetPromiseType(Guid id)
		=> m_Store.TryGetValue(id, out var p)
			? p.GetType().GetGenericArguments()[0]
			: null;
}
