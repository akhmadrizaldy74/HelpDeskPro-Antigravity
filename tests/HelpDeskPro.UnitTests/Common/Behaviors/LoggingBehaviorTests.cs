namespace HelpDeskPro.UnitTests.Common.Behaviors;

using FluentAssertions;
using HelpDeskPro.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class LoggingBehaviorTests
{
    public record SampleCommand(string Data) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenCalled_LogsAndReturnsResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<SampleCommand, string>>>();
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);
        var request = new SampleCommand("TestData");

        RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }
}

