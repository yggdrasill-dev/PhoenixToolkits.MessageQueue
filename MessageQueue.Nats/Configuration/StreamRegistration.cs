using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class StreamRegistration
{
	private readonly Action<StreamConfiguration.StreamConfigurationBuilder> m_ConfigureCallback;

	public StreamRegistration(Action<StreamConfiguration.StreamConfigurationBuilder> configure)
	{
		m_ConfigureCallback = configure;
	}

	public void Configure(StreamConfiguration.StreamConfigurationBuilder builder)
		=> m_ConfigureCallback(builder);
}
