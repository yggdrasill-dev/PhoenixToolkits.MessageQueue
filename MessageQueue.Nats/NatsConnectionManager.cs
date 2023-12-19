using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

internal class NatsConnectionManager : INatsConnectionManager
{
	private readonly NatsConnection m_NatsConnection;

	public INatsConnection Connection => m_NatsConnection;

	public NatsConnectionManager(NatsConnection natsConnection)
	{
		m_NatsConnection = natsConnection ?? throw new ArgumentNullException(nameof(natsConnection));
	}

	public INatsJSContext CreateJsContext()
		=> new NatsJSContext(m_NatsConnection);

	public IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry,
		string? sessionReplySubject)
		=> ActivatorUtilities.CreateInstance<NatsMessageSender>(
			serviceProvider,
			natsSerializerRegistry!,
			sessionReplySubject!,
			Connection);

	public IMessageSender CreateJetStreamMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry)
		=> ActivatorUtilities.CreateInstance<JetStreamMessageSender>(
			serviceProvider,
			natsSerializerRegistry!,
			CreateJsContext());
}
