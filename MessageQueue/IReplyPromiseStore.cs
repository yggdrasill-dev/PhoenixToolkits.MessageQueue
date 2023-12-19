namespace Valhalla.MessageQueue;

public interface IReplyPromiseStore
{
	(Guid Id, Task<Answer<TReply>> Promise) CreatePromise<TReply>(CancellationToken cancellationToken);

	void SetCanceled(Guid id);

	void SetException(Guid id, Exception ex);

	void SetResult<TReply>(Guid id, Answer<TReply> answer);

	Type? GetPromiseType(Guid id);
}
