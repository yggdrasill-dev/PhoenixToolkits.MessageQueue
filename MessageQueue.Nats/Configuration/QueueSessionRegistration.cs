using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueSessionRegistration<TMessageSession> : ISubscribeRegistration
	where TMessageSession : IMessageSession
{
	private readonly bool m_IsSession;

	public string Queue { get; }

	public string Subject { get; }

	public QueueSessionRegistration(string subject, string queue, bool isSession = true)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;
		m_IsSession = isSession;
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<NatsSubscriptionSettings> messageReceiver,
		IMessageReceiver<NatsQueueScriptionSettings> queueReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> queueReceiver.SubscribeAsync(new NatsQueueScriptionSettings
		{
			Subject = Subject,
			Queue = Queue,
			EventHandler = (sender, args) => HandleMessageAsync(new MessageDataInfo
			{
				Args = args,
				ServiceProvider = serviceProvider,
				Logger = logger,
				CancellationToken = cancellationToken
			}).AsTask()
		});

	private static async Task ProcessMessageAsync(
		Question question,
		TMessageSession handler,
		CancellationToken cancellationToken)
	{
		try
		{
			await handler
				.HandleAsync(question, cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			if (question.CanResponse)
				await question.FailAsync(
					Encoding.UTF8.GetBytes(ex.ToString()),
					cancellationToken).ConfigureAwait(false);

			throw;
		}
	}

	private Question CreateQuestion(MessageDataInfo dataInfo, INatsMessageQueueService natsSender)
			=> m_IsSession
			? new NatsQuestion(
				dataInfo.Args.Message.Data,
				natsSender,
				dataInfo.Args.Message.Reply)
			: new NatsAction(
				dataInfo.Args.Message.Data);

	private async ValueTask HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.Message.Header,
			(header, key) => header[key],
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				Subject,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				})
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				});

		try
		{
#pragma warning disable CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
			await using var scope = dataInfo.ServiceProvider.CreateAsyncScope();
#pragma warning restore CA2007 // 請考慮對等候的工作呼叫 ConfigureAwait
			var handler = ActivatorUtilities.CreateInstance<TMessageSession>(scope.ServiceProvider);
			var natsSender = scope.ServiceProvider.GetRequiredService<INatsMessageQueueService>();
			var question = CreateQuestion(dataInfo, natsSender);

			await ProcessMessageAsync(
				question,
				handler,
				dataInfo.CancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}
}
