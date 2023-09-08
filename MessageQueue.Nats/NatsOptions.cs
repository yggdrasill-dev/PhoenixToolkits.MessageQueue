namespace Valhalla.MessageQueue.Nats;

public class NatsOptions
{
	public int? MaxReconnect { get; set; }

	public string Url { get; set; } = "nats://localhost:4222";
}
