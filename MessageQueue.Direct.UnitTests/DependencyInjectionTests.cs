using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessageQueue.Direct.UnitTests;

public class DependencyInjectionTests
{
	[Fact]
	public void 註冊InProcessMessageQueue()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => { });
	}

	[Fact]
	public void 註冊一個MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddHandler<StubMessageHandler<string>>("a.b.c"));
	}

	[Fact]
	public void 以MessageHanderType註冊MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddHandler(typeof(StubMessageHandler<string>), "a.b.c"));
	}

	[Fact]
	public void 註冊一個MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddProcessor<StubMessageProcessor<string, string>>("a.b.c"));
	}

	[Fact]
	public void 以MessageProcessorType註冊MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddProcessor(typeof(StubMessageProcessor<string, string>), "a.b.c"));
	}

	[Fact]
	public void 註冊一個MessageSession()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddSession<StubMessageSession<string>>("a.b.c"));
	}

	[Fact]
	public void 以MessageSessionType註冊MessageSession()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddSession(typeof(StubMessageSession<string>), "a.b.c"));
	}
}
