using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
	private readonly Func<IServiceProvider, TReceiver> m_ReceiverFactory;
	private readonly IServiceProvider m_ServiceProvider;
	private bool m_DisposedValue;

	public MessageSubscriber(
		Func<IServiceProvider, TReceiver> receiverFactory,
		IServiceProvider serviceProvider,
		ILogger<MessageSubscriber<TMessage, TReceiver>> logger)
	{
		m_ReceiverFactory = receiverFactory ?? throw new ArgumentNullException(nameof(receiverFactory));
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

		try
		{
			var payload = MessageSubscriber<TMessage, TReceiver>.GetMongoMessage<TMessage>(processContext);

			Task.Run(async () =>
			{
				var scope = m_ServiceProvider.CreateAsyncScope();
				await using (scope.ConfigureAwait(false))
				{
					var receiver = m_ReceiverFactory(scope.ServiceProvider);

					await receiver.HandleAsync(
						payload.Subject,
						payload.Data!,
						payload.Headers).ConfigureAwait(false);
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

	private static MongoMessage<T> GetMongoMessage<T>(ProcessContext processContext)
	{
		if (typeof(T) == typeof(Memory<byte>))
		{
			var payload = processContext.Data<MongoMessage<byte[]>>();

			var messageData = new Memory<byte>(payload.Data);

			return new MongoMessage<T>
			{
				Headers = payload.Headers,
				Subject = payload.Subject,
				TraceParent = payload.TraceParent,
				TraceState = payload.TraceState,
				Data = Unsafe.As<Memory<byte>, T>(ref messageData),
			};
		}

		if (typeof(T) == typeof(ReadOnlyMemory<byte>))
		{
			var payload = processContext.Data<MongoMessage<byte[]>>();

			var messageData = new ReadOnlyMemory<byte>(payload.Data);

			return new MongoMessage<T>
			{
				Headers = payload.Headers,
				Subject = payload.Subject,
				TraceParent = payload.TraceParent,
				TraceState = payload.TraceState,
				Data = Unsafe.As<ReadOnlyMemory<byte>, T>(ref messageData),
			};
		}

		return processContext.Data<MongoMessage<T>>();
	}
}
