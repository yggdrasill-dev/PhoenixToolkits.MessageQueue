using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessageQueue.Nats.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void 如果註冊同樣Type的實體_取得IEnumerable應該得到所有實體()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton("1")
            .AddSingleton("2")
            .AddSingleton("3");

        var sut = services.BuildServiceProvider();

        var expected = new[]
        {
            "1",
            "2",
            "3"
        };

        // Act
        var actual = sut.GetRequiredService<IEnumerable<string>>();

        // Assert
        Assert.Equal(expected, actual);
    }
}
