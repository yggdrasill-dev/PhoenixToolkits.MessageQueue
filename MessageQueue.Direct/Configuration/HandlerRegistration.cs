using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal class HandlerRegistration<THandler> : ISubscribeRegistration
	where THandler : class, IMessageHandler
{
	public Glob SubjectGlob { get; }

	public HandlerRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<DirectHandlerMessageSender<THandler>>();
}
