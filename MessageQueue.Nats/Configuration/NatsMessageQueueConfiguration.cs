using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.Nats.Configuration;

public class NatsMessageQueueConfiguration
{
	internal static ActivitySource _NatsActivitySource = new("Valhalla.MessageQueue.Nats");

	private readonly MessageQueueConfiguration m_CoreConfiguration;
	private readonly List<ISubscribeRegistration> m_SubscribeRegistrations = new();

	public IServiceCollection Services { get; }

	internal string? SessionReplySubject => m_CoreConfiguration.SessionReplySubject;

	public NatsMessageQueueConfiguration(MessageQueueConfiguration coreConfiguration)
	{
		Services = coreConfiguration.Services;

		m_CoreConfiguration = coreConfiguration;

		_ = Services.AddSingleton<IEnumerable<ISubscribeRegistration>>(m_SubscribeRegistrations);
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(string subject) where THandler : IMessageHandler
	{
		m_SubscribeRegistrations.Add(new SubscribeRegistration<THandler>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(Type handlerType, string subject)
	{
		var registrationType = typeof(SubscribeRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(string subject, string group) where THandler : IMessageHandler
	{
		m_SubscribeRegistrations.Add(new QueueRegistration<THandler>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(Type handlerType, string subject, string group)
	{
		var registrationType = typeof(QueueRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(string subject) where TProcessor : IMessageProcessor
	{
		m_SubscribeRegistrations.Add(new ProcessRegistration<TProcessor>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(Type processorType, string subject)
	{
		var registrationType = typeof(ProcessRegistration<>).MakeGenericType(processorType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(string subject, string queue) where TProcessor : IMessageProcessor
	{
		m_SubscribeRegistrations.Add(new QueueProcessRegistration<TProcessor>(subject, queue));

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(Type processorType, string subject, string queue)
	{
		var registrationType = typeof(QueueProcessRegistration<>).MakeGenericType(processorType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, queue)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(string subject) where THandler : IMessageHandler
	{
		m_SubscribeRegistrations.Add(new ReplyRegistration<THandler>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(Type handlerType, string subject)
	{
		var registrationType = typeof(ReplyRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(string subject, string group) where THandler : IMessageHandler
	{
		m_SubscribeRegistrations.Add(new QueueReplyRegistration<THandler>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(Type handlerType, string subject, string group)
	{
		var registrationType = typeof(QueueReplyRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TSession>(string subject) where TSession : IMessageSession
	{
		m_SubscribeRegistrations.Add(new SessionRegistration<TSession>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(Type sessionType, string subject)
	{
		var registrationType = typeof(SessionRegistration<>).MakeGenericType(sessionType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for session type {sessionType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TSession>(string subject, string group) where TSession : IMessageSession
	{
		m_SubscribeRegistrations.Add(new QueueSessionRegistration<TSession>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(Type sessionType, string subject, string group)
	{
		var registrationType = typeof(QueueSessionRegistration<>).MakeGenericType(sessionType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for session type {sessionType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration ConfigQueueOptions(Action<NatsOptions, IServiceProvider> configure)
	{
		_ = Services
			.AddOptions<NatsOptions>()
			.Configure(configure);

		return this;
	}

	public NatsMessageQueueConfiguration HandleNatsMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<ExceptionHandler>(
			sp,
			handleException));

		return this;
	}

	public NatsMessageQueueConfiguration SetSessionReplySubject(string subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' 不得為 Null 或空白字元。", nameof(subject));

		m_SubscribeRegistrations.Add(new SessionReplyRegistration(subject));
		_ = m_CoreConfiguration.RegisterSessionReplySubject(subject);

		return this;
	}
}
