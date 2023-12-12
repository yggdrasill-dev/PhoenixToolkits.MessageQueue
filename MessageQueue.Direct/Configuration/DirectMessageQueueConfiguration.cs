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

	public DirectMessageQueueConfiguration AddHandler<TMessage, THandler>(string subject)
		where THandler : class, IMessageHandler<TMessage>
	{
		_SubscribeRegistrations.Add(new HandlerRegistration<TMessage, THandler>(Glob.Parse(subject)));

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

	public DirectMessageQueueConfiguration AddProcessor<TMessage, TReply, TProcessor>(string subject) where TProcessor
		: class, IMessageProcessor<TMessage, TReply>
	{
		_SubscribeRegistrations.Add(new ProcessorRegistration<TMessage, TReply, TProcessor>(Glob.Parse(subject)));

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

	public DirectMessageQueueConfiguration AddSession<TMessage, TSession>(string subject)
		where TSession : class, IMessageSession<TMessage>
	{
		_SubscribeRegistrations.Add(new SessionRegistration<TMessage, TSession>(Glob.Parse(subject)));

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
