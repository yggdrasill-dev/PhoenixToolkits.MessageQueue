using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Messaging;
using MongoDB.Messaging.Subscription;
using Valhalla.MessageQueue.MongoDB.Configuration;

namespace Valhalla.MessageQueue.MongoDB;

internal class MessageSubscriber<TMessage, TReceiver> : IMessageSubscriber, IAsyncDisposable
	where TReceiver : class, IMessageHandler<TMessage>
{
	private readonly ILogger<MessageSubscriber<TMessage, TReceiver>> m_Logger;
	private readonly IServiceProvider m_ServiceProvider;
	private bool m_DisposedValue;

	public MessageSubscriber(IServiceProvider serviceProvider, ILogger<MessageSubscriber<TMessage, TReceiver>> logger)
	{
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore().ConfigureAwait(false);

		Dispose(false);

		GC.SuppressFinalize(this);
	}

	public MessageResult Process(ProcessContext processContext)
	{
		using var activity = TraceContextPropagator.TryExtract(
			processContext.Data<MongoMessage<byte[]>>(),
			(p, key) => key switch
			{
				TraceContextPropagator.TraceParent => p.TraceParent!,
				TraceContextPropagator.TraceState => p.TraceState!,
				_ => string.Empty
			},
			out var context)
			? MongoDBMessageQueueConfiguration._MongoActivitySource.StartActivity(
				processContext.Container.Name,
				ActivityKind.Server,
				context,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "MongoDB"),
					new KeyValuePair<string, object?>("handler", typeof(TReceiver).Name)
				})
			: MongoDBMessageQueueConfiguration._MongoActivitySource.StartActivity(
				ActivityKind.Server,
				name: processContext.Container.Name,
				tags: new[]
				{
					new KeyValuePair<string, object?>("mq", "MongoDB"),
					new KeyValuePair<string, object?>("handler", typeof(TReceiver).Name)
				});

		var payload = processContext.Data<MongoMessage<TMessage>>();

		try
		{
			Task.Run(async () =>
			{
				var scope = m_ServiceProvider.CreateAsyncScope();
				await using (scope.ConfigureAwait(false))
				{
					var receiver = scope.ServiceProvider.GetRequiredService<TReceiver>();

					await receiver.HandleAsync(payload!.Data!).ConfigureAwait(false);
				}
			}).GetAwaiter().GetResult();

			return MessageResult.Successful;
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			m_Logger.LogError(ex, "Process fail.");

			Task.Run(async () =>
			{
				foreach (var handler in m_ServiceProvider.GetServices<ExceptionHandler>())
					await handler.HandleExceptionAsync(
						ex).ConfigureAwait(false);
			}).GetAwaiter().GetResult();

			return MessageResult.Error;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
			}

			m_DisposedValue = true;
		}
	}

	protected virtual ValueTask DisposeAsyncCore()
	{
		return ValueTask.CompletedTask;
	}
}
