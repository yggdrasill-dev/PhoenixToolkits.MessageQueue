using Microsoft.Extensions.DependencyInjection;

namespace MessageQueue.InProcess.UnitTests;

public class DependencyInjectionTests
{
	[Fact]
	public void 註冊InProcessMessageQueue()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddInProcessMessageQueue(config => { });
	}

	[Fact]
	public void 註冊一個MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddInProcessMessageQueue(config => config
				.AddHandler<StubMessageHandler<string>>("a.b.c"));
	}

	[Fact]
	public void 以MessageHanderType註冊MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddInProcessMessageQueue(config => config
				.AddHandler(typeof(StubMessageHandler<string>), "a.b.c"));
	}

	[Fact]
	public void 註冊一個MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddInProcessMessageQueue(config => config
				.AddProcessor<StubMessageProcessor<string, string>>("a.b.c"));
	}

	[Fact]
	public void 以MessageProcessorType註冊MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddInProcessMessageQueue(config => config
				.AddProcessor(typeof(StubMessageProcessor<string, string>), "a.b.c"));
	}
}
