using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Valhalla.MessageQueue.Nats;

internal interface INatsConnectionManager
{
	INatsConnection Connection { get; }

	INatsJSContext CreateJsContext();
}
