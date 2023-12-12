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

	public DirectMessageQueueConfiguration AddHandler<THandler>(string subject)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject);

		return this;
	}

	public DirectMessageQueueConfiguration AddHandler(Type handlerType, string subject)
	{
		var typeArguments = handlerType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var registrationType = typeof(HandlerRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor<TProcessor>(string subject)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(processorType, subject);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor(Type processorType, string subject)
	{
		var typeArguments = processorType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var registrationType = typeof(ProcessorRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddSession<TSession>(string subject)
	{
		var sessionType = typeof(TSession);

		AddSession(sessionType, subject);

		return this;
	}

	public DirectMessageQueueConfiguration AddSession(Type sessionType, string subject)
	{
		var typeArguments = sessionType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageSession<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(sessionType)
			.ToArray();

		var registrationType = typeof(SessionRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
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
