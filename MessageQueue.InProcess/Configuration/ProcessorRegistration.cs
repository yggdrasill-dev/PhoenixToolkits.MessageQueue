using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Configuration;

internal class ProcessorRegistration<TProcessor> : ISubscribeRegistration
	where TProcessor : class, IMessageProcessor
{
	public Glob SubjectGlob { get; }

	public ProcessorRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<MessageProcessorExecutor<TProcessor>>(
			serviceProvider,
			ActivatorUtilities.CreateInstance<TProcessor>(serviceProvider));
}
