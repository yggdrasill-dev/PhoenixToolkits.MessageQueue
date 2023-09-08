namespace Valhalla.MessageQueue;

public class MessageSenderNotFoundException : Exception
{
	public string Subject { get; }

	public MessageSenderNotFoundException(string subject)
		: base($"The subject({subject}) can't find sender.")
	{
		Subject = subject;
	}
}
