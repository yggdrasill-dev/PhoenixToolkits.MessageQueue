using System.Reflection;
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

	public InProcessMessageQueueConfiguration AddHandler<THandler>(string subject, Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, handlerFactory);

		return this;
	}

	public InProcessMessageQueueConfiguration AddHandler(Type handlerType, string subject, Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
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

		m_SubscribeRegistrations.Add(registration);

		return this;
	}

	public InProcessMessageQueueConfiguration AddProcessor<TProcessor>(string subject, Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(processorType, subject, processorFactory);

		return this;
	}

	public InProcessMessageQueueConfiguration AddProcessor(Type processorType, string subject, Delegate? processorFactory = null)
	{
		var typeArguments = processorType.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
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
