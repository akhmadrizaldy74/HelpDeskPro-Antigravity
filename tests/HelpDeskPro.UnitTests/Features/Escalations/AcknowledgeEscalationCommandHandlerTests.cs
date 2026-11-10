namespace HelpDeskPro.UnitTests.Features.Escalations;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Escalations.Commands.AcknowledgeEscalation;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class AcknowledgeEscalationCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AcknowledgeEscalationCommandHandler _handler;

    private readonly Guid _supervisorId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public AcknowledgeEscalationCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_supervisorId.ToString());
        _currentUserService.Role.Returns("Supervisor");

        _handler = new AcknowledgeEscalationCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_RecipientAcknowledgesEscalation_ShouldSucceed()
    {
        // Arrange
        var supervisor = new ApplicationUser { Id = _supervisorId, FullName = "Hendra Supervisor" };
        var escalation = new Escalation
        {
            Id = 1,
            TicketId = 10,
            Level = 1,
            Reason = EscalationReason.SlaResponseBreach,
            EscalatedToId = _supervisorId,
            EscalatedTo = supervisor,
            EscalatedAt = _utcNow.AddHours(-1)
        };

        var escalationsSet = new List<Escalation> { escalation }.AsTestDbSet();
        _context.Escalations.Returns(escalationsSet);

        var command = new AcknowledgeEscalationCommand(escalation.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AcknowledgedAt.Should().Be(_utcNow);
        escalation.AcknowledgedAt.Should().Be(_utcNow);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnauthorizedUserAttemptsAcknowledge_ThrowsBusinessRuleException()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns(otherUserId.ToString());
        _currentUserService.Role.Returns("Agent");

        var supervisor = new ApplicationUser { Id = _supervisorId, FullName = "Hendra Supervisor" };
        var escalation = new Escalation
        {
            Id = 1,
            TicketId = 10,
            Level = 1,
            Reason = EscalationReason.SlaResponseBreach,
            EscalatedToId = _supervisorId,
            EscalatedTo = supervisor,
            EscalatedAt = _utcNow.AddHours(-1)
        };

        var escalationsSet = new List<Escalation> { escalation }.AsTestDbSet();
        _context.Escalations.Returns(escalationsSet);

        var command = new AcknowledgeEscalationCommand(escalation.Id);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Hanya penerima eskalasi yang dapat menandai eskalasi sudah ditindaklanjuti.");
    }

    [Fact]
    public async Task Handle_EscalationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var escalationsSet = new List<Escalation>().AsTestDbSet();
        _context.Escalations.Returns(escalationsSet);

        var command = new AcknowledgeEscalationCommand(999);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

