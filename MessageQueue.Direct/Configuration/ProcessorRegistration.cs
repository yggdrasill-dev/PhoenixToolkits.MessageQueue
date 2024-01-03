using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

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

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectProcessorMessageSender<TMessage, TReply, TProcessor>>(
			serviceProvider,
			m_ProcessorFactory);
}
