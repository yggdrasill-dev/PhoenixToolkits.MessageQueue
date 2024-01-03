using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.InProcess.Configuration;

internal class ProcessorRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	private readonly Func<IServiceProvider, TProcessor> m_ProcessorFactory;

	public Glob SubjectGlob { get; }

	public ProcessorRegistration(Glob subjectGlob, Func<IServiceProvider, TProcessor> processorFactory)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
		m_ProcessorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
	}

	public IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<MessageProcessorExecutor<TMessage, TReply, TProcessor>>(
			serviceProvider,
			m_ProcessorFactory(serviceProvider));
}
