using DotNet.Globbing;
using Valhalla.MessageQueue.Direct.Configuration;

namespace Valhalla.MessageQueue.Direct;

internal class DirectMessageExchange : IMessageExchange
{
	private readonly Glob m_Glob;
	private readonly IEnumerable<ISubscribeRegistration> m_SubscribeRegistrations;

	public DirectMessageExchange(string pattern, IEnumerable<ISubscribeRegistration> subscribeRegistrations)
	{
		m_Glob = Glob.Parse(pattern);

		m_SubscribeRegistrations = subscribeRegistrations ?? throw new ArgumentNullException(nameof(subscribeRegistrations));
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		foreach (var sub in m_SubscribeRegistrations)
			if (sub.SubjectGlob.IsMatch(subject))
				return sub.ResolveMessageSender(serviceProvider);

		throw new MessageSenderNotFoundException(subject);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}
