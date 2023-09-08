using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.InProcess.Configuration;

public class InProcessMessageQueueConfiguration
{
	private readonly List<ISubscribeRegistration> m_SubscribeRegistrations = new();

	public IServiceCollection Services { get; }

	public InProcessMessageQueueConfiguration(MessageQueueConfiguration coreConfiguration)
	{
		Services = coreConfiguration.Services;

		_ = Services.AddSingleton<IEnumerable<ISubscribeRegistration>>(m_SubscribeRegistrations);
	}

	public InProcessMessageQueueConfiguration AddHandler<THandler>(string subject) where THandler : class, IMessageHandler
	{
		m_SubscribeRegistrations.Add(new HandlerRegistration<THandler>(Glob.Parse(subject)));

		return this;
	}

	public InProcessMessageQueueConfiguration AddHandler(Type handlerType, string subject)
	{
		var registrationType = typeof(HandlerRegistration<>).MakeGenericType(handlerType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public InProcessMessageQueueConfiguration AddProcessor<TProcessor>(string subject) where TProcessor : class, IMessageProcessor
	{
		m_SubscribeRegistrations.Add(new ProcessorRegistration<TProcessor>(Glob.Parse(subject)));

		return this;
	}

	public InProcessMessageQueueConfiguration AddProcessor(Type processorType, string subject)
	{
		var registrationType = typeof(ProcessorRegistration<>).MakeGenericType(processorType);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject))
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public InProcessMessageQueueConfiguration HandleInProcessMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<ExceptionHandler>(
			sp,
			handleException));

		return this;
	}
}
