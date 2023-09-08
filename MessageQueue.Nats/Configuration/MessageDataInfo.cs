using Microsoft.Extensions.Logging;
using NATS.Client;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class MessageDataInfo
{
	public MsgHandlerEventArgs Args { get; init; } = default!;

	public CancellationToken CancellationToken { get; init; }

	public ILogger Logger { get; init; } = default!;

	public IServiceProvider ServiceProvider { get; init; } = default!;
}
