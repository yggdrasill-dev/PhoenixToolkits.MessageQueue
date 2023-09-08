using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client;
using Valhalla.MessageQueue;
using Valhalla.MessageQueue.Configuration;
using Valhalla.MessageQueue.Nats;
using Valhalla.MessageQueue.Nats.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFakeNatsMessageQueue(this IServiceCollection services)
	{
		foreach (var desc in services.Where(
			desc => desc.ServiceType == typeof(IHostedService)
				&& desc.ImplementationType == typeof(MessageQueueBackground)).ToArray())
		{
			_ = services.Remove(desc);
		}

		return services
			.AddSingleton<INatsMessageQueueService, NoopMessageQueueService>()
			.AddTransient<IMessageReceiver<NatsSubscriptionSettings>>(
				sp => sp.GetRequiredService<NoopMessageQueueService>())
			.AddTransient<IMessageReceiver<NatsQueueScriptionSettings>>(
				sp => sp.GetRequiredService<NoopMessageQueueService>());
	}

	public static MessageQueueConfiguration AddNatsMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<NatsMessageQueueConfiguration> configure)
	{
		var natsConfiguration = new NatsMessageQueueConfiguration(configuration);

		configure(natsConfiguration);

		InitialNatsMessageQueueConfiguration(natsConfiguration);

		return configuration;
	}

	private static StringBuilder BeginFormatMessage(string label, Connection? conn, Subscription? sub, string? error)
	{
		StringBuilder stringBuilder = new StringBuilder(label);
		if (conn != null)
		{
			_ = stringBuilder.Append(", Connection: ").Append(conn.ClientID);
		}

		if (sub != null)
		{
			_ = stringBuilder.Append(", Subscription: ").Append(sub.Sid);
		}

		if (error != null)
		{
			_ = stringBuilder.Append(", Error: ").Append(error);
		}

		return stringBuilder;
	}

	private static void InitialNatsMessageQueueConfiguration(NatsMessageQueueConfiguration configuration)
	{
		_ = configuration.Services
			.AddSingleton<ConnectionFactory>()
			.AddSingleton<INatsMessageQueueService>(sp =>
			{
				var connectionFactory = sp.GetRequiredService<ConnectionFactory>();
				var queueOptions = sp.GetRequiredService<IOptions<NatsOptions>>().Value;
				var logger = sp.GetRequiredService<ILogger<NatsOptions>>();

				logger.LogDebug("Nats connection url: {queueOptions.Url}", queueOptions.Url);

				var options = ConnectionFactory.GetDefaultOptions();

				options.Url = queueOptions.Url;
				options.MaxReconnect = queueOptions.MaxReconnect ?? 1000;
				options.AsyncErrorEventHandler = (sender, args) => logger.WriteError("AsyncErrorEvent", args);
				options.ClosedEventHandler = (sender, args) => logger.WriteEvent("ClosedEvent", args);
				options.DisconnectedEventHandler = (sender, args) => logger.WriteEvent("DisconnectedEvent", args);
				options.FlowControlProcessedEventHandler = (sender, args) => logger.WriteEvent(
					"FlowControlProcessed",
					args,
					"FcSubject: ",
					args?.FcSubject,
					"Source: ",
					args?.Source);
				options.HeartbeatAlarmEventHandler = (sender, args) => logger.WriteEvent("HeartbeatAlarmEvent", args);
				options.LameDuckModeEventHandler = (sender, args) => logger.WriteEvent("LameDuckModeEvent", args);
				options.ReconnectDelayHandler = (sender, args) => logger.LogInformation(
					"ReconnectDelay Attempts: {attempts}",
					args.Attempts);
				options.ReconnectedEventHandler = (sender, args) => logger.WriteEvent("ReconnectedEvent", args);
				options.ServerDiscoveredEventHandler = (sender, args) => logger.WriteEvent("ServerDiscoveredEvent", args);
				options.UnhandledStatusEventHandler = (sender, args) => logger.WriteEvent(
					"UnhandledStatus",
					args,
					"Status: ",
					args?.Status);

				return new NatsMessageQueueService(
					connectionFactory.CreateConnection(options),
					configuration.SessionReplySubject,
					sp.GetRequiredService<IReplyPromiseStore>(),
					sp.GetRequiredService<ILogger<NatsMessageQueueService>>());
			})
			.AddTransient<IMessageReceiver<NatsSubscriptionSettings>>(
				sp => sp.GetRequiredService<INatsMessageQueueService>())
			.AddTransient<IMessageReceiver<NatsQueueScriptionSettings>>(
				sp => sp.GetRequiredService<INatsMessageQueueService>())
			.AddHostedService<MessageQueueBackground>()
			.AddOptions<NatsOptions>();
	}

	private static void WriteError<T>(this ILogger<T> logger, string label, ErrEventArgs? e)
	{
		logger.LogError("{msg}", e == null
			? label
			: BeginFormatMessage(label, e.Conn, e.Subscription, e.Error).ToString());
	}

	private static void WriteEvent<T>(this ILogger<T> logger, string label, ConnJsSubEventArgs e, params object?[] pairs)
	{
		StringBuilder stringBuilder = BeginFormatMessage(label, e?.Conn, e?.Sub, null);
		int num;
		for (num = 0; num < pairs.Length; num++)
		{
			_ = stringBuilder.Append(", ").Append(pairs[num]).Append(pairs[++num]);
		}

		logger.LogInformation("{msg}", stringBuilder.ToString());
	}

	private static void WriteEvent<T>(this ILogger<T> logger, string label, ConnEventArgs? e)
	{
		logger.LogInformation("{msg}", e == null
			? label
			: BeginFormatMessage(label, e.Conn, null, e.Error?.Message).ToString());
	}
}
