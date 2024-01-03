using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Valhalla.MessageQueue.Configuration;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

public class MongoDBMessageQueueConfiguration
{
	internal static ActivitySource _MongoActivitySource = new("Valhalla.MessageQueue.Mongo");

	public IServiceCollection Services { get; }

	public MongoDBMessageQueueConfiguration(MessageQueueConfiguration messageQueueConfiguration)
	{
		Services = messageQueueConfiguration.Services;
	}

	public MongoDBMessageQueueBuilder DefineMessageQueue(
		string managerName,
		Func<IServiceProvider, MongoMessageQueueBuilderOptions> messageQueueFactory)
	{
		var builder = new MongoDBMessageQueueBuilder(
			managerName,
			messageQueueFactory);

		_ = Services.AddSingleton(builder);

		return builder;
	}

	public MongoDBMessageQueueConfiguration HandleMongoMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<ExceptionHandler>(
			sp,
			handleException));

		return this;
	}
}
