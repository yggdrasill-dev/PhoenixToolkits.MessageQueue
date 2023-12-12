using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal class ProcessorRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	public Glob SubjectGlob { get; }

	public ProcessorRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<DirectProcessorMessageSender<TMessage, TReply, TProcessor>>();
}
