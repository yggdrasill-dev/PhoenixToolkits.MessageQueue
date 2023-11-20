using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class StreamRegistration
{
	private readonly Action<StreamConfiguration.StreamConfigurationBuilder> m_ConfigureCallback;

	public string Name { get; }

	public StreamRegistration(string name, Action<StreamConfiguration.StreamConfigurationBuilder> configure)
	{
		Name = name;
		m_ConfigureCallback = configure;
	}

	public void Configure(StreamConfiguration.StreamConfigurationBuilder builder)
		=> m_ConfigureCallback(builder);
}
