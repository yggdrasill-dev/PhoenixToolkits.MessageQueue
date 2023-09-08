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

	public DirectMessageQueueConfiguration AddHandler<THandler>(string subject) where THandler : class, IMessageHandler
	{
		_SubscribeRegistrations.Add(new HandlerRegistration<THandler>(Glob.Parse(subject)));

		return this;
	}

	public DirectMessageQueueConfiguration AddHandler(Type handlerType, string subject)
	{
		var registrationType = typeof(HandlerRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor<TProcessor>(string subject) where TProcessor : class, IMessageProcessor
	{
		_SubscribeRegistrations.Add(new ProcessorRegistration<TProcessor>(Glob.Parse(subject)));

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor(Type processorType, string subject)
	{
		var registrationType = typeof(ProcessorRegistration<>).MakeGenericType(processorType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddSession<TSession>(string subject) where TSession : class, IMessageSession
	{
		_SubscribeRegistrations.Add(new SessionRegistration<TSession>(Glob.Parse(subject)));

		return this;
	}

	public DirectMessageQueueConfiguration AddSession(Type sessionType, string subject)
	{
		var registrationType = typeof(SessionRegistration<>).MakeGenericType(sessionType);
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
