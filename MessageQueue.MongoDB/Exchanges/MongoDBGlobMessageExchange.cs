using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Valhalla.MessageQueue.MongoDB.Exchanges;

internal class MongoDBGlobMessageExchange : IMessageExchange
{
	private readonly Glob m_Glob;
	private readonly string m_ManagerName;
	private readonly string m_QueueName;

	public MongoDBGlobMessageExchange(string pattern, string managerName, string queueName)
	{
		if (string.IsNullOrWhiteSpace(managerName))
			throw new ArgumentException($"'{nameof(managerName)}' 不得為 Null 或空白字元。", nameof(managerName));
		if (string.IsNullOrWhiteSpace(queueName))
			throw new ArgumentException($"'{nameof(queueName)}' 不得為 Null 或空白字元。", nameof(queueName));

		m_Glob = Glob.Parse(pattern);
		m_ManagerName = managerName;
		m_QueueName = queueName;
	}

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var queueManagerService = serviceProvider.GetRequiredService<QueueManagerService>();

		var queueManager = queueManagerService[m_ManagerName];

		return new MongoDbMessageQueueService(queueManager, m_QueueName);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header) => m_Glob.IsMatch(subject);
}
