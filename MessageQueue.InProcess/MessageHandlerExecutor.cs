namespace Valhalla.MessageQueue.InProcess;

internal class MessageHandlerExecutor<TMessage, THandler> : IMessageHandlerExecutor
	where THandler : class, IMessageHandler<TMessage>
{
	private readonly THandler m_Handler;

	public MessageHandlerExecutor(THandler handler)
	{
		m_Handler = handler ?? throw new ArgumentNullException(nameof(handler));
	}

	public async ValueTask HandleAsync(InProcessMessage message, CancellationToken cancellationToken = default)
	{
		await m_Handler.HandleAsync(
			message.Subject,
			(TMessage)message.Message!,
			cancellationToken).ConfigureAwait(false);

		message.CompletionSource.SetResult(null);
	}
}
