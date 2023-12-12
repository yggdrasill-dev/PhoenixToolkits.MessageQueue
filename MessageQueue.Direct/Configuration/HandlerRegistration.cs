using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal class HandlerRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : class, IMessageHandler<TMessage>
{
	public Glob SubjectGlob { get; }

	public HandlerRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<DirectHandlerMessageSender<TMessage, THandler>>();
}
