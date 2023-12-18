using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

public readonly struct NatsAcknowledgeMessage<TMessage> : IAcknowledgeMessage<TMessage>
{
	private readonly NatsJSMsg<TMessage> m_Msg;

	public TMessage? Data => m_Msg.Data;

	public string Subject => m_Msg.Subject;

	public NatsAcknowledgeMessage(NatsJSMsg<TMessage> msg)
	{
		m_Msg = msg;
	}

	public ValueTask AckAsync(CancellationToken cancellationToken = default)
		=> m_Msg.AckAsync(cancellationToken: cancellationToken);

	public ValueTask AckProgressAsync(CancellationToken cancellationToken = default)
		=> m_Msg.AckProgressAsync(cancellationToken: cancellationToken);

	public ValueTask AckTerminateAsync(CancellationToken cancellationToken = default)
		=> m_Msg.AckTerminateAsync(cancellationToken: cancellationToken);

	public ValueTask NakAsync(TimeSpan delay = default, CancellationToken cancellationToken = default)
		=> m_Msg.NakAsync(delay: delay, cancellationToken: cancellationToken);
}
