using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.Nats.Configuration;

public class NatsMessageQueueConfiguration
{
	internal static ActivitySource _NatsActivitySource = new("Valhalla.MessageQueue.Nats");

	private readonly MessageQueueConfiguration m_CoreConfiguration;
	private readonly List<StreamConfig> m_StreamRegistrations = new();
	private readonly List<ISubscribeRegistration> m_SubscribeRegistrations = new();

	public IServiceCollection Services { get; }

	internal string? SessionReplySubject => m_CoreConfiguration.SessionReplySubject;

	public NatsMessageQueueConfiguration(MessageQueueConfiguration coreConfiguration)
	{
		Services = coreConfiguration.Services;

		m_CoreConfiguration = coreConfiguration;

		_ = Services
			.AddSingleton<IEnumerable<ISubscribeRegistration>>(m_SubscribeRegistrations)
			.AddSingleton<IEnumerable<StreamConfig>>(m_StreamRegistrations);
	}

	public NatsMessageQueueConfiguration ConfigureResolveConnection(Func<IServiceProvider, NatsConnection> configure)
	{
		Services
			.AddSingleton<INatsMessageQueueService>(sp => new NatsMessageQueueService(
				new NatsConnectionManager(configure(sp)),
				SessionReplySubject,
				sp.GetRequiredService<IReplyPromiseStore>(),
				sp.GetRequiredService<ILogger<NatsMessageQueueService>>()))
			.AddHostedService<MessageQueueBackground>();

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<TMessage, THandler>(string subject)
		where THandler : IMessageHandler<TMessage>
	{
		m_SubscribeRegistrations.Add(new SubscribeRegistration<TMessage, THandler>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(Type handlerType, string subject)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(SubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<TMessage, THandler>(string subject, string group)
		where THandler : IMessageHandler<TMessage>
	{
		m_SubscribeRegistrations.Add(new QueueRegistration<TMessage, THandler>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(Type handlerType, string subject, string group)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(QueueRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler<TMessage, THandler>(
		string subject,
		string stream,
		ConsumerConfig consumerConfig)
		where THandler : INatsMessageHandler<TMessage>
	{
		m_SubscribeRegistrations.Add(new JetStreamHandlerRegistration<TMessage, THandler>(
			subject,
			stream,
			consumerConfig));

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler(
		Type handlerType,
		string subject,
		string stream,
		ConsumerConfig consumerConfig)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(INatsMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(JetStreamHandlerRegistration<,>).MakeGenericType(typeArguments);

		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, stream, consumerConfig)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TMessage, TReply, TProcessor>(string subject)
		where TProcessor : IMessageProcessor<TMessage, TReply>
	{
		m_SubscribeRegistrations.Add(new ProcessRegistration<TMessage, TReply, TProcessor>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(Type processorType, string subject)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var registrationType = typeof(ProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TMessage, TReply, TProcessor>(string subject, string queue)
		where TProcessor : IMessageProcessor<TMessage, TReply>
	{
		m_SubscribeRegistrations.Add(new QueueProcessRegistration<TMessage, TReply, TProcessor>(subject, queue));

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(Type processorType, string subject, string queue)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var registrationType = typeof(QueueProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, queue)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<TMessage, THandler>(string subject)
		where THandler : IMessageHandler<TMessage>
	{
		m_SubscribeRegistrations.Add(new ReplyRegistration<TMessage, THandler>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(Type handlerType, string subject)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(ReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<TMessage, THandler>(string subject, string group)
		where THandler : IMessageHandler<TMessage>
	{
		m_SubscribeRegistrations.Add(new QueueReplyRegistration<TMessage, THandler>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(Type handlerType, string subject, string group)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(QueueReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TMessage, TSession>(string subject)
		where TSession : IMessageSession<TMessage>
	{
		m_SubscribeRegistrations.Add(new SessionRegistration<TMessage, TSession>(subject));

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(Type sessionType, string subject)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var registrationType = typeof(SessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject)
			?? throw new InvalidOperationException($"Unable to create a registration for session type {sessionType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TMessage, TSession>(string subject, string group) where TSession : IMessageSession<TMessage>
	{
		m_SubscribeRegistrations.Add(new QueueSessionRegistration<TMessage, TSession>(subject, group));

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(Type sessionType, string subject, string group)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var registrationType = typeof(QueueSessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group)
			?? throw new InvalidOperationException($"Unable to create a registration for session type {sessionType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration ConfigJetStream(StreamConfig config)
	{
		m_StreamRegistrations.Add(config);

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

		m_SubscribeRegistrations.Add(new SessionReplyRegistration<ReadOnlyMemory<byte>>(subject));
		_ = m_CoreConfiguration.RegisterSessionReplySubject(subject);

		return this;
	}
}
