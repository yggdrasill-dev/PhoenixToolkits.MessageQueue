using DotNet.Globbing;

namespace Valhalla.MessageQueue.InProcess.Configuration;

internal interface ISubscribeRegistration
{
	Glob SubjectGlob { get; }

	IMessageHandlerExecutor CreateExecutor(IServiceProvider serviceProvider);
}
