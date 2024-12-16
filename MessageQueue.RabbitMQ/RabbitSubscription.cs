using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitSubscription : IDisposable
{
	private readonly IChannel m_Channel;
	private readonly IConnection m_Connection;
	private readonly string m_ConsumerTag;
	private readonly ILogger<RabbitSubscription> m_Logger;
	private readonly string m_Subject;
	private bool m_DisposedValue;

	public RabbitSubscription(
		string subject,
		IConnection connection,
		IChannel channel,
		string consumerTag,
		ILogger<RabbitSubscription> logger)
	{
		m_Subject = subject;
		m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		m_ConsumerTag = consumerTag;
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

		m_Connection.CallbackExceptionAsync += Connection_CallbackExceptionAsync;
		m_Connection.ConnectionBlockedAsync += Connection_ConnectionBlockedAsync;
		m_Connection.ConnectionShutdownAsync += Connection_ConnectionShutdownAsync;
		m_Connection.ConnectionUnblockedAsync += Connection_ConnectionUnblockedAsync;

		m_Channel.CallbackExceptionAsync += Connection_CallbackExceptionAsync;
		m_Channel.CallbackExceptionAsync += Connection_CallbackExceptionAsync;
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				m_Channel.BasicCancelAsync(m_ConsumerTag);

				m_Connection.Dispose();
				m_Channel.Dispose();
			}

			m_DisposedValue = true;
		}
	}

	private Task Connection_ConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
	{
		m_Logger.LogInformation("Receiver({Subject}) ConnectionShutdownEvent", m_Subject);
		return Task.CompletedTask;
	}

	private Task Connection_ConnectionUnblockedAsync(object? sender, AsyncEventArgs e)
	{
		m_Logger.LogInformation("Receiver({Subject}) ConnectionUnblockedEvent", m_Subject);
		return Task.CompletedTask;
	}

	private Task Connection_ConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
	{
		m_Logger.LogInformation("Receiver({Subject}) ConnectionBlockedEvent, Reason: {Reason}", m_Subject, e.Reason);
		return Task.CompletedTask;
	}

	private Task Connection_CallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
	{
		m_Logger.LogError(e.Exception, "Receiver({Subject}) CallbackExceptionEvent", m_Subject);
		return Task.CompletedTask;
	}
}
