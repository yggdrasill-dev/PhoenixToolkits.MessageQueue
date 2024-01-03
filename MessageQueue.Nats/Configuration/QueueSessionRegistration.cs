using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace Valhalla.MessageQueue.Nats.Configuration;

internal class QueueSessionRegistration<TMessage, TMessageSession> : ISubscribeRegistration
	where TMessageSession : IMessageSession<TMessage>
{
	private readonly bool m_IsSession;
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;
	private readonly Func<IServiceProvider, TMessageSession> m_MessageSessionFactory;

	public string Queue { get; }

	public string Subject { get; }

	public QueueSessionRegistration(
		string subject,
		string queue,
		bool isSession,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, TMessageSession> messageSessionFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		if (string.IsNullOrEmpty(queue))
			throw new ArgumentException($"'{nameof(queue)}' is not Null or Empty.", nameof(queue));
		Subject = subject;
		Queue = queue;
		m_IsSession = isSession;
		m_NatsSerializerRegistry = natsSerializerRegistry;
		m_MessageSessionFactory = messageSessionFactory;
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new NatsQueueScriptionSettings<TMessage>(
				Subject,
				Queue,
				(args, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<TMessage>>(
						args,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);

	private static async Task ProcessMessageAsync(
		Question<TMessage> question,
		TMessageSession handler,
		CancellationToken cancellationToken)
	{
		try
		{
			await handler
				.HandleAsync(question, cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception)
		{
			if (question.CanResponse)
				await question.FailAsync(
					"Process message occur error",
					cancellationToken).ConfigureAwait(false);

			throw;
		}
	}

	private Question<TMessage> CreateQuestion(MessageDataInfo<NatsMsg<TMessage>> dataInfo, IMessageSender messageSender)
		=> m_IsSession
			? new NatsQuestion<TMessage>(
				dataInfo.Msg.Subject,
				dataInfo.Msg.Data!,
				dataInfo.Msg.Headers?
					.SelectMany(kv => kv.Value
						.Select(v => new MessageHeaderValue(kv.Key, v))),
				messageSender,
				dataInfo.Msg.ReplyTo)
			: new NatsAction<TMessage>(
				dataInfo.Msg.Subject,
				dataInfo.Msg.Data!,
				dataInfo.Msg.Headers?
					.SelectMany(kv => kv.Value
						.Select(v => new MessageHeaderValue(kv.Key, v))));

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsMsg<TMessage>> dataInfo, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Msg.Headers,
			(header, key) => (header?[key] ?? string.Empty)!,
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
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var handler = m_MessageSessionFactory(scope.ServiceProvider);
				var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
				var question = CreateQuestion(dataInfo, messageSender);

				await ProcessMessageAsync(
					question,
					handler,
					cts.Token).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}
