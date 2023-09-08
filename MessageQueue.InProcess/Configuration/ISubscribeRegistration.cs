using DotNet.Globbing;

namespace Valhalla.MessageQueue.Configuration;

internal interface ISubscribeRegistration
{
	Glob SubjectGlob { get; }

	IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider);
}
