using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.Direct.Configuration;

public class DirectMessageQueueConfiguration
{
	private static readonly List<ISubscribeRegistration> _SubscribeRegistrations = new();

	public IServiceCollection Services { get; }

	internal static IEnumerable<ISubscribeRegistration> SubscribeRegistrations => _SubscribeRegistrations;

	public DirectMessageQueueConfiguration(MessageQueueConfiguration messageQueueConfiguration)
	{
		Services = messageQueueConfiguration.Services;
	}

	public DirectMessageQueueConfiguration AddHandler<THandler>(string subject, Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, handlerFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddHandler(Type handlerType, string subject, Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(HandlerRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject), factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor<TProcessor>(string subject, Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(processorType, subject, processorFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor(Type processorType, string subject, Delegate? processorFactory = null)
	{
		var typeArguments = processorType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ProcessorRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject), factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddSession<TSession>(string subject, Func<IServiceProvider, TSession>? sessionFactory = null)
	{
		var sessionType = typeof(TSession);

		AddSession(sessionType, subject, sessionFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddSession(Type sessionType, string subject, Delegate? sessionFactory = null)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var factory = sessionFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(sessionType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(SessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject), factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {sessionType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration HandleDirectMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<ExceptionHandler>(
			sp,
			handleException));

		return this;
	}
}
