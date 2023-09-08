namespace Valhalla.MessageQueue;

public class MessageProcessFailException : Exception
{
	public byte[]? ResponseData { get; }

	public MessageProcessFailException()
		: this(null)
	{
	}

	public MessageProcessFailException(byte[]? responseData)
		: base("MessageQueue remote process fail.")
	{
		ResponseData = responseData;
	}
}
