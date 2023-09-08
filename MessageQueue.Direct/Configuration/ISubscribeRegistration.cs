using DotNet.Globbing;

namespace Valhalla.MessageQueue.Direct.Configuration;

internal interface ISubscribeRegistration
{
	Glob SubjectGlob { get; }

	IMessageSender ResolveMessageSender(IServiceProvider serviceProvider);
}
