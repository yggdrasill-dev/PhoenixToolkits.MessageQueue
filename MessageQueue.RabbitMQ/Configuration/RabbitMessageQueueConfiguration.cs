using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.RabbitMQ.Configuration;

public class RabbitMessageQueueConfiguration
{
	internal static ActivitySource _RabbitActivitySource = new("Valhalla.MessageQueue.RabbitMQ");

	private readonly List<ISubscribeRegistration> m_SubscribeRegistrations = new();

	public IServiceCollection Services { get; }

	public RabbitMessageQueueConfiguration(IServiceCollection services)
	{
		Services = services ?? throw new ArgumentNullException(nameof(services));

		_ = services.AddMessageQueue();

		_ = Services.AddSingleton<IEnumerable<ISubscribeRegistration>>(m_SubscribeRegistrations);
	}

	public RabbitMessageQueueConfiguration(MessageQueueConfiguration coreConfiguration)
	{
		if (coreConfiguration is null)
			throw new ArgumentNullException(nameof(coreConfiguration));

		Services = coreConfiguration.Services;

		_ = Services.AddSingleton<IEnumerable<ISubscribeRegistration>>(m_SubscribeRegistrations);
	}

	public RabbitMessageQueueConfiguration AddHandler<THandler>(string queueName, bool autoAck = true, int dispatchConcurrency = 1)
		where THandler : IMessageHandler<ReadOnlyMemory<byte>>
	{
		m_SubscribeRegistrations.Add(
			new SubscribeRegistration<THandler>(queueName, autoAck, dispatchConcurrency));

		return this;
	}

	public RabbitMessageQueueConfiguration AddHandler(
		Type handlerType,
		string queueName,
		bool autoAck = true,
		int dispatchConcurrency = 1)
	{
		var registrationType = typeof(SubscribeRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			queueName,
			autoAck,
			dispatchConcurrency)
			?? throw new InvalidOperationException(
				$"Unable to create registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public RabbitMessageQueueConfiguration ConfigQueueOptions(Action<RabbitMQOptions, IServiceProvider> configure)
	{
		_ = Services
			.AddOptions<RabbitMQOptions>()
			.Configure(configure);

		return this;
	}

	public RabbitMessageQueueConfiguration HandleRabbitMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<ExceptionHandler>(
			sp,
			handleException));

		return this;
	}
}
