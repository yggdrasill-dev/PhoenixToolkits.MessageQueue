using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal record MessageDataInfo<TNatsMsg>(
	TNatsMsg Msg,
	ILogger Logger,
	IServiceProvider ServiceProvider);
