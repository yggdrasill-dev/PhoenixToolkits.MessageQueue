using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Valhalla.MessageQueue.RabbitMQ;

internal class RabbitMQConnectionManager : IDisposable, IMessageReceiver<RabbitSubscriptionSettings>
{
	internal static readonly ActivitySource _RabbitMQActivitySource = new("Valhalla.MessageQueue.RabbitMQ");

	private readonly IConnection m_Connection;
	private readonly ILogger<RabbitMQConnectionManager> m_Logger;
	private readonly RabbitMQOptions m_Options;
	private readonly IServiceProvider m_ServiceProvider;
	private bool m_DisposedValue;

	public IModel MessageQueueChannel { get; }

	public RabbitMQConnectionManager(
		IServiceProvider serviceProvider,
		IOptions<RabbitMQOptions> optionsAccessor,
		ILogger<RabbitMQConnectionManager> logger)
	{
		m_Options = optionsAccessor.Value;
		m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

		var factory = new ConnectionFactory()
		{
			Uri = m_Options.RabbitMQUrl,
			UserName = m_Options.UserName,
			Password = m_Options.Password,
			VirtualHost = m_Options.VirtualHost,
			DispatchConsumersAsync = true
		};

		m_Connection = factory.CreateConnection();
		m_Connection.CallbackException += Connection_CallbackException;
		m_Connection.ConnectionBlocked += Connection_ConnectionBlocked;
		m_Connection.ConnectionShutdown += Connection_ConnectionShutdown;
		m_Connection.ConnectionUnblocked += Connection_ConnectionUnblocked;

		MessageQueueChannel = m_Connection.CreateModel();

		MessageQueueChannel.CallbackException += Connection_CallbackException;
		MessageQueueChannel.ModelShutdown += MessageQueueChannel_ModelShutdown;

		m_Options.OnSetupQueueAndExchange?.Invoke(MessageQueueChannel);
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);

		GC.SuppressFinalize(this);
	}

	public ValueTask<IDisposable> SubscribeAsync(RabbitSubscriptionSettings settings)
	{
		var factory = new ConnectionFactory()
		{
			Uri = m_Options.RabbitMQUrl,
			UserName = m_Options.UserName,
			Password = m_Options.Password,
			VirtualHost = m_Options.VirtualHost,
			DispatchConsumersAsync = true,
			ConsumerDispatchConcurrency = settings.ConsumerDispatchConcurrency
		};

		var connection = factory.CreateConnection();
		var channel = connection.CreateModel();
		var consumer = new AsyncEventingBasicConsumer(channel);
		consumer.Received += (sender, args) => settings.EventHandler(channel, args);

		var consumerTag = channel.BasicConsume(
			queue: settings.Subject,
			autoAck: settings.AutoAck,
			consumer: consumer);

		return ValueTask.FromResult<IDisposable>(
			new RabbitSubscription(
				settings.Subject,
				connection,
				channel,
				consumerTag,
				m_ServiceProvider.GetRequiredService<ILogger<RabbitSubscription>>()));
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				m_Logger.LogWarning($"{nameof(RabbitMQConnectionManager)} disposing.");

				MessageQueueChannel?.Close();
				MessageQueueChannel?.Dispose();

				m_Connection.Close();
				m_Connection.Dispose();
			}

			m_DisposedValue = true;
		}
	}

	private void Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
	{
		m_Logger.LogError(e.Exception, "Sender CallbackExceptionEvent");
	}

	private void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
	{
		m_Logger.LogInformation("Sender ConnectionBlockedEvent, Reason: {reason}", e.Reason);
	}

	private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
	{
		m_Logger.LogInformation("Sender ConnectionShutdownEvent");
	}

	private void Connection_ConnectionUnblocked(object? sender, EventArgs e)
	{
		m_Logger.LogInformation("Sender ConnectionUnblockedEvent");
	}

	private void MessageQueueChannel_ModelShutdown(object? sender, ShutdownEventArgs e)
	{
		m_Logger.LogInformation("Sender ModelShutdownEvent");
	}
}
