using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal class HandlerRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : class, IMessageHandler<TMessage>
{
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public Glob SubjectGlob { get; }

	public HandlerRegistration(Glob subjectGlob, Func<IServiceProvider, THandler> handlerFactory)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
		m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
	}

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectHandlerMessageSender<TMessage, THandler>>(
			serviceProvider,
			m_HandlerFactory);
}
