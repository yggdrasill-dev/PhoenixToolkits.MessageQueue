namespace Valhalla.MessageQueue;

public class MessageProcessFailException : Exception
{
	public string? ResponseData { get; }

	public MessageProcessFailException()
		: this(null)
	{
	}

	public MessageProcessFailException(string? responseData)
		: base("MessageQueue remote process fail.")
	{
		ResponseData = responseData;
	}
}
