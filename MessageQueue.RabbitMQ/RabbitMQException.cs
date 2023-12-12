namespace Valhalla.MessageQueue.RabbitMQ;
public class RabbitMQException : Exception
{
	public RabbitMQException()
	{
	}

	public RabbitMQException(string message) : base(message)
	{
	}

	public RabbitMQException(string message, Exception inner) : base(message, inner)
	{
	}
}
