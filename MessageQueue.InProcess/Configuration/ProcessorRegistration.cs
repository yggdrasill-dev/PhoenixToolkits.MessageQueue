using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.InProcess.Configuration;

internal class ProcessorRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	public Glob SubjectGlob { get; }

	public ProcessorRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<MessageProcessorExecutor<TMessage, TReply, TProcessor>>(
			serviceProvider,
			ActivatorUtilities.CreateInstance<TProcessor>(serviceProvider));
}
