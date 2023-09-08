using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.InProcess.Configuration;

internal class HandlerRegistration<THandler> : ISubscribeRegistration
	where THandler : class, IMessageHandler
{
	public Glob SubjectGlob { get; }

	public HandlerRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<MessageHandlerExecutor<THandler>>(
			serviceProvider,
			ActivatorUtilities.CreateInstance<THandler>(serviceProvider));
}
