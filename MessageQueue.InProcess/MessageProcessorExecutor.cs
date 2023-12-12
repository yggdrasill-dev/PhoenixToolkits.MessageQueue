namespace Valhalla.MessageQueue.InProcess;

internal class MessageProcessorExecutor<TMessage, TReply, TProcessor> : IMessageHandlerExecutor
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	private readonly TProcessor m_Processor;

	public MessageProcessorExecutor(TProcessor processor)
	{
		m_Processor = processor ?? throw new ArgumentNullException(nameof(processor));
	}

	public async ValueTask HandleAsync(InProcessMessage message, CancellationToken cancellationToken = default)
	{
		var response = await m_Processor.HandleAsync(
			(TMessage)message.Message!,
			cancellationToken).ConfigureAwait(false);

		message.CompletionSource.SetResult(response);
	}
}
