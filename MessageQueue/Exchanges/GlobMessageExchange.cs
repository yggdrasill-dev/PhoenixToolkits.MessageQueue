using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.Exchanges;

internal class GlobMessageExchange<TMessageSender> : IMessageExchange
	where TMessageSender : class, IMessageSender
{
	private readonly Glob m_Glob;

	public GlobMessageExchange(string pattern)
	{
		m_Glob = Glob.Parse(pattern);
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<TMessageSender>();

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}
