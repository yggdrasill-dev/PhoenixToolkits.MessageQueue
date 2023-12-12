using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal class SessionRegistration<TMessage, TSession> : ISubscribeRegistration
	where TSession : IMessageSession<TMessage>
{
	public Glob SubjectGlob { get; }

	public SessionRegistration(Glob subjectGlob)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
	}

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<DirectSessionMessageSender<TMessage, TSession>>();
}
