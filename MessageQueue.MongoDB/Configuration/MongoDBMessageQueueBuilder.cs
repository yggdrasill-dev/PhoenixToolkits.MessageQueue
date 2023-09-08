﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Messaging;
using MongoDB.Messaging.Service;
using MongoDB.Messaging.Subscription;

namespace Valhalla.MessageQueue.MongoDB.Configuration;

public class MongoDBMessageQueueBuilder
{
	private readonly Func<IServiceProvider, MongoMessageQueueBuilderOptions> m_Factory;
	private readonly List<QueueDeclare> m_QueueDeclares = new();
	private readonly IServiceCollection m_Services;
	private readonly List<SubscribeRegistration> m_Subscriptions = new();

	public string Name { get; }

	public MongoDBMessageQueueBuilder(
		IServiceCollection services,
		string name,
		Func<IServiceProvider, MongoMessageQueueBuilderOptions> factory)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException($"'{nameof(name)}' 不得為 Null 或空白字元。", nameof(name));
		m_Services = services ?? throw new ArgumentNullException(nameof(services));
		Name = name;
		m_Factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public MongoDBMessageQueueBuilder AddHandler<THandler>(string queueName, TimeSpan pollTime, int workers = 1)
		where THandler : class, IMessageHandler
		=> AddHandler(typeof(THandler), queueName, pollTime, workers);

	public MongoDBMessageQueueBuilder AddHandler(Type handlerType, string queueName, TimeSpan pollTime, int workers = 1)
	{
		m_Services.TryAddTransient(handlerType);

		m_Subscriptions.Add(new SubscribeRegistration
		{
			QueueName = queueName,
			PollTime = pollTime,
			Workers = workers,
			HandlerType = handlerType
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
								var subscriberType = typeof(MessageSubscriber<>);

								var handlerType = subscriberType.MakeGenericType(registration.HandlerType);

								return serviceProvider.GetRequiredService(handlerType) as IMessageSubscriber;
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
