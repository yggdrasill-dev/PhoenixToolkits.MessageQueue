using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.InProcess.Configuration;

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

	public IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<MessageHandlerExecutor<TMessage, THandler>>(
			serviceProvider,
			m_HandlerFactory(serviceProvider));
}
