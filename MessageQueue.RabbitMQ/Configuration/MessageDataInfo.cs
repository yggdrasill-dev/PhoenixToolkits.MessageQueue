using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ.Configuration;

internal class MessageDataInfo
{
	public BasicDeliverEventArgs Args { get; init; } = default!;

	public CancellationToken CancellationToken { get; init; }

	public IModel Channel { get; init; } = default!;

	public ILogger Logger { get; init; } = default!;

	public IServiceProvider ServiceProvider { get; init; } = default!;
}
