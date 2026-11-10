namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.AddComment;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class AddCommentCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly AddCommentCommandHandler _handler;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Department _itDept = new() { Id = 1, Code = "IT", Name = "IT" };
    private readonly Department _hrDept = new() { Id = 2, Code = "HR", Name = "HR" };

    public AddCommentCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        _handler = new AddCommentCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService);
    }

    private Ticket CreateSampleTicket(long id = 1)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester", Department = _hrDept };
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 240 };

        var ticket = Ticket.Create(
            "HD-2026-000001",
            "Monitor Rusak",
            "Monitor mati total",
            1,
            TicketPriority.High,
            requester.Id,
            slaPolicy,
            _utcNow.AddHours(-1),
            "requester@helpdeskpro.local");

        ticket.Id = id;
        ticket.Category = category;
        ticket.Requester = requester;

        return ticket;
    }

    [Fact]
    public async Task Handle_FirstCommentByAgent_ShouldFillFirstRespondedAt_FR17()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        ticket.FirstRespondedAt.Should().BeNull();

        var agent = new ApplicationUser { Id = _agentId, FullName = "Doni Agent", Department = _itDept };
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agent }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AddCommentCommand(ticket.Id, "Sedang kami periksa", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Body.Should().Be("Sedang kami periksa");
        ticket.FirstRespondedAt.Should().Be(_utcNow);
        ticket.Comments.Should().ContainSingle(c => c.Body == "Sedang kami periksa");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SecondCommentByAgent_PreservesExistingFirstRespondedAt()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var initialRespondedAt = _utcNow.AddMinutes(-30);
        ticket.FirstRespondedAt = initialRespondedAt;

        var agent = new ApplicationUser { Id = _agentId, FullName = "Doni Agent", Department = _itDept };
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agent }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AddCommentCommand(ticket.Id, "Komentar lanjutan", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        ticket.FirstRespondedAt.Should().Be(initialRespondedAt);
    }

    [Fact]
    public async Task Handle_CommentByRequester_DoesNotFillFirstRespondedAt()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester", Department = _hrDept };
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { requester }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AddCommentCommand(ticket.Id, "Tolong segera dicek", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        ticket.FirstRespondedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RequesterAttemptsInternalComment_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester", Department = _hrDept };
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { requester }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AddCommentCommand(ticket.Id, "Komentar rahasia", true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Requester tidak diizinkan membuat komentar internal.");
    }

    [Fact]
    public async Task Handle_RequesterAccessingOtherUsersTicket_ThrowsNotFoundException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var otherRequesterId = Guid.NewGuid();
        _currentUserService.UserId.Returns(otherRequesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var command = new AddCommentCommand(ticket.Id, "Komentar", false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

