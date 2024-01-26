using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
			.AddSingleton<INatsConnectionManager>(sp => new NatsConnectionManager(configure(sp)))
			.AddSingleton<INatsMessageQueueService>(sp => new NatsMessageQueueService(
				sp.GetRequiredService<INatsConnectionManager>()))
			.AddHostedService<MessageQueueBackground>();

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(
		Type handlerType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(SubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, group, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(
		Type handlerType,
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler<THandler>(
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddJetStreamHandler(
			handlerType,
			subject,
			stream,
			consumerConfig,
			natsSerializerRegistry,
			handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler(
		Type handlerType,
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IAcknowledgeMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(JetStreamHandlerRegistration<,>).MakeGenericType(typeArguments);

		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType,
			subject,
			stream,
			consumerConfig,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(
			processorType,
			subject,
			natsSerializerRegistry,
			processorFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(
		Type processorType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? processorFactory = null)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(
			processorType,
			subject,
			queue,
			natsSerializerRegistry,
			processorFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(
		Type processorType,
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? processorFactory = null)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, queue, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddReplyHandler(handlerType, subject, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(
		Type handlerType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddReplyHandler(
			handlerType,
			subject,
			group,
			natsSerializerRegistry,
			handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(
		Type handlerType,
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, group, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TSession>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TSession>? sessionFactory = null)
	{
		var sessionType = typeof(TSession);

		AddSession(sessionType, subject, natsSerializerRegistry, sessionFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(
		Type sessionType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? sessionFactory = null)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var factory = sessionFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(sessionType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(SessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, subject, true, natsSerializerRegistry, factory)
			?? throw new InvalidOperationException($"Unable to create a registration for session type {sessionType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession<TMessage, TSession>(
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TSession>? sessionFactory = null)
	{
		var sessionType = typeof(TSession);

		AddSession(sessionType, subject, group, natsSerializerRegistry, sessionFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddSession(
		Type sessionType,
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? sessionFactory = null)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var factory = sessionFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(sessionType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueSessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			subject,
			group,
			true,
			natsSerializerRegistry,
			factory)
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

	public NatsMessageQueueConfiguration SetSessionReplySubject(string subject, INatsSerializerRegistry? natsSerializerRegistry = null)
	{
		if (string.IsNullOrWhiteSpace(subject))
			throw new ArgumentException($"'{nameof(subject)}' 不得為 Null 或空白字元。", nameof(subject));

		m_SubscribeRegistrations.Add(new SessionReplyRegistration(subject, natsSerializerRegistry));
		_ = m_CoreConfiguration.RegisterSessionReplySubject(subject);

		return this;
	}
}
