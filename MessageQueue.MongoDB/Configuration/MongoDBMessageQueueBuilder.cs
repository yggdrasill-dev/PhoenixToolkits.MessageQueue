using Microsoft.Extensions.DependencyInjection;
using MongoDB.Messaging;
using MongoDB.Messaging.Service;
using MongoDB.Messaging.Subscription;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

public class MongoDBMessageQueueBuilder
{
	private readonly Func<IServiceProvider, MongoMessageQueueBuilderOptions> m_Factory;
	private readonly List<QueueDeclare> m_QueueDeclares = new();
	private readonly List<SubscribeRegistration> m_Subscriptions = new();

	public string Name { get; }

	public MongoDBMessageQueueBuilder(
		string name,
		Func<IServiceProvider, MongoMessageQueueBuilderOptions> factory)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException($"'{nameof(name)}' 不得為 Null 或空白字元。", nameof(name));
		Name = name;
		m_Factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public MongoDBMessageQueueBuilder AddHandler<THandler>(
		string queueName,
		TimeSpan pollTime,
		int workers = 1,
		Func<IServiceProvider, THandler>? handlerFactory = null)
		=> AddHandler(
			typeof(THandler),
			queueName,
			pollTime,
			workers,
			handlerFactory);

	public MongoDBMessageQueueBuilder AddHandler(
		Type handlerType,
		string queueName,
		TimeSpan pollTime,
		int workers = 1,
		Delegate? handlerFactory = null)
	{
		var factory = handlerFactory
			?? (Delegate)typeof(DefaultHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null)!;

		m_Subscriptions.Add(new SubscribeRegistration
		{
			QueueName = queueName,
			PollTime = pollTime,
			Workers = workers,
			HandlerType = handlerType,
			HandlerFactory = factory
		});

		return this;
	}

	public MongoDBMessageQueueBuilder DeclareQueue(
		string queueName,
		MessagePriority priority = MessagePriority.Normal,
		int retry = 0)
	{
		m_QueueDeclares.Add(new QueueDeclare(queueName, priority, retry));

		return this;
	}

	internal MessageService Build(IServiceProvider serviceProvider)
	{
		var manager = m_Factory(serviceProvider);
		manager.MessageQueue
			.Configure(builder =>
			{
				_ = builder.ConnectionString(manager.ConnectionString);

				foreach (var declare in m_QueueDeclares)
					_ = builder.Queue(
						queueBuilder => queueBuilder
							.Name(declare.Name)
							.Retry(declare.Retry)
							.Priority(declare.Priority));

				foreach (var registration in m_Subscriptions)
					_ = builder.Subscribe(
						subBuilder => subBuilder
							.Queue(registration.QueueName)
							.Handler(() =>
							{
								var subscriberType = typeof(MessageSubscriber<,>);
								var typeArguments = registration.HandlerType
									.GetInterfaces()
									.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
									.Take(1)
									.SelectMany(t => t.GetGenericArguments())
									.Append(registration.HandlerType)
									.ToArray();

								var handlerType = subscriberType.MakeGenericType(typeArguments);

								return ActivatorUtilities.CreateInstance(
									serviceProvider,
									handlerType,
									registration.HandlerFactory) as IMessageSubscriber;
							})
							.PollTime(registration.PollTime)
							.Workers(registration.Workers)
							.Trigger());
			});

		var managerService = serviceProvider.GetRequiredService<QueueManagerService>();

		managerService.Add(Name, manager.MessageQueue);

		return new MessageService(manager.MessageQueue.QueueManager);
	}
}
