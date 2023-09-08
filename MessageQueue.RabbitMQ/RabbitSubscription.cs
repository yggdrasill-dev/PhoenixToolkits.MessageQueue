using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitSubscription : IDisposable
{
	private readonly IModel m_Channel;
	private readonly IConnection m_Connection;
	private readonly string m_ConsumerTag;
	private readonly ILogger<RabbitSubscription> m_Logger;
	private readonly string m_Subject;
	private bool m_DisposedValue;

	public RabbitSubscription(
		string subject,
		IConnection connection,
		IModel channel,
		string consumerTag,
		ILogger<RabbitSubscription> logger)
	{
		m_Subject = subject;
		m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		m_ConsumerTag = consumerTag;
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

		m_Connection.CallbackException += Connection_CallbackException;
		m_Connection.ConnectionBlocked += Connection_ConnectionBlocked;
		m_Connection.ConnectionShutdown += Connection_ConnectionShutdown;
		m_Connection.ConnectionUnblocked += Connection_ConnectionUnblocked;

		m_Channel.CallbackException += Connection_CallbackException;
		m_Channel.ModelShutdown += Channel_ModelShutdown;
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
				m_Channel.BasicCancel(m_ConsumerTag);

				m_Connection.Dispose();
				m_Channel.Dispose();
			}

			m_DisposedValue = true;
		}
	}

	private void Channel_ModelShutdown(object? sender, ShutdownEventArgs e)
	{
		m_Logger.LogInformation("Receiver({_subject}) ModelShutdownEvent", m_Subject);
	}

	private void Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
	{
		m_Logger.LogError(e.Exception, "Receiver({_subject}) CallbackExceptionEvent", m_Subject);
	}

	private void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
	{
		m_Logger.LogInformation("Receiver({_subject}) ConnectionBlockedEvent, Reason: {e.Reason}", m_Subject, e.Reason);
	}

	private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
	{
		m_Logger.LogInformation("Receiver({_subject}) ConnectionShutdownEvent", m_Subject);
	}

	private void Connection_ConnectionUnblocked(object? sender, EventArgs e)
	{
		m_Logger.LogInformation("Receiver({_subject}) ConnectionUnblockedEvent", m_Subject);
	}
}
