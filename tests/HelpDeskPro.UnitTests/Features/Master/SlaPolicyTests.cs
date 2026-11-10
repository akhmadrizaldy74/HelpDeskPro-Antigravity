namespace HelpDeskPro.UnitTests.Features.Master;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Commands.UpdateSlaPolicy;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Queries.GetSlaPolicies;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class SlaPolicyTests
{
    private readonly IApplicationDbContext _context;

    public SlaPolicyTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task GetSlaPolicies_ReturnsAllPolicies()
    {
        // Arrange
        var policies = new List<SlaPolicy>
        {
            new() { Id = 1, Priority = TicketPriority.Critical, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 240 },
            new() { Id = 2, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 }
        };

        var policySet = policies.AsTestDbSet();
        _context.SlaPolicies.Returns(policySet);

        var handler = new GetSlaPoliciesQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetSlaPoliciesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Priority.Should().Be(TicketPriority.Critical);
    }

    [Fact]
    public async Task UpdateSlaPolicy_WhenValid_UpdatesPolicyValues()
    {
        // Arrange
        var policy = new SlaPolicy { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 };
        var policySet = new List<SlaPolicy> { policy }.AsTestDbSet();
        _context.SlaPolicies.Returns(policySet);

        var handler = new UpdateSlaPolicyCommandHandler(_context);

        // Act
        var result = await handler.Handle(new UpdateSlaPolicyCommand(TicketPriority.High, 60, 360), CancellationToken.None);

        // Assert
        result.ResponseTimeMinutes.Should().Be(60);
        result.ResolutionTimeMinutes.Should().Be(360);
        policy.ResponseTimeMinutes.Should().Be(60);
        policy.ResolutionTimeMinutes.Should().Be(360);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpdateSlaPolicyValidator_WhenResolutionLessThanResponse_FailsValidation()
    {
        // Arrange
        var validator = new UpdateSlaPolicyCommandValidator();
        var command = new UpdateSlaPolicyCommand(TicketPriority.Critical, 120, 60); // 60 < 120

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSlaPolicyCommand.ResolutionTimeMinutes));
    }
}
