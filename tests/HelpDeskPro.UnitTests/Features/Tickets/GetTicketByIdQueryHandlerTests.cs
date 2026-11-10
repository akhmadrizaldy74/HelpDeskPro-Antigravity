namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetTicketByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly GetTicketByIdQueryHandler _handler;

    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public GetTicketByIdQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);

        _handler = new GetTicketByIdQueryHandler(
            _context,
            _currentUserService,
            _dateTimeProvider);
    }

    private Ticket CreateSampleTicketWithTimeline()
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester", DepartmentId = 1 };
        var agent = new ApplicationUser { Id = _agentId, FullName = "Doni Agent", DepartmentId = 3 };

        var ticket = new Ticket
        {
            Id = 10,
            TicketNo = "HD-2026-000010",
            Title = "Laptop Tidak Bisa Booting",
            Description = "Layar biru saat booting",
            CategoryId = 1,
            Category = category,
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            RequesterId = _requesterId,
            Requester = requester,
            AssigneeId = _agentId,
            Assignee = agent,
            ResponseDueAt = _utcNow.AddHours(2),
            ResolutionDueAt = _utcNow.AddHours(8),
            IsSlaBreached = false,
            CreatedAt = _utcNow.AddHours(-2)
        };

        // Comments: 1 public comment, 1 internal comment
        ticket.Comments.Add(new TicketComment
        {
            Id = 1,
            TicketId = 10,
            AuthorId = _requesterId,
            Author = requester,
            Body = "Komentar publik dari requester",
            IsInternal = false,
            CreatedAt = _utcNow.AddMinutes(-90)
        });

        ticket.Comments.Add(new TicketComment
        {
            Id = 2,
            TicketId = 10,
            AuthorId = _agentId,
            Author = agent,
            Body = "Catatan internal teknisi: perlu ganti SSD",
            IsInternal = true,
            CreatedAt = _utcNow.AddMinutes(-60)
        });

        // History: Ticket created, Status changed
        ticket.Histories.Add(new TicketHistory
        {
            Id = 1,
            TicketId = 10,
            FieldName = "Ticket",
            OldValue = null,
            NewValue = "Created",
            ChangedById = _requesterId,
            ChangedBy = requester,
            ChangedAt = _utcNow.AddHours(-2)
        });

        ticket.Histories.Add(new TicketHistory
        {
            Id = 2,
            TicketId = 10,
            FieldName = "Status",
            OldValue = "Open",
            NewValue = "InProgress",
            ChangedById = _agentId,
            ChangedBy = agent,
            ChangedAt = _utcNow.AddMinutes(-80)
        });

        return ticket;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);

        // Act
        var act = () => _handler.Handle(new GetTicketByIdQuery(10), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _currentUserService.UserId.Returns(_requesterId.ToString());
        var tickets = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var act = () => _handler.Handle(new GetTicketByIdQuery(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tiket*");
    }

    [Fact]
    public async Task Handle_RequesterAccessingOthersTicket_ShouldThrowNotFoundException_404()
    {
        // Arrange - Current user is _otherUserId with Requester role
        _currentUserService.UserId.Returns(_otherUserId.ToString());
        _currentUserService.Role.Returns("Requester");

        var ticket = CreateSampleTicketWithTimeline();
        var tickets = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var act = () => _handler.Handle(new GetTicketByIdQuery(10), CancellationToken.None);

        // Assert - Must throw NotFoundException (404) to avoid leaking ticket existence
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tiket*");
    }

    [Fact]
    public async Task Handle_RequesterAccessingOwnTicket_HidesInternalComments_AC05()
    {
        // Arrange
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var ticket = CreateSampleTicketWithTimeline();
        var tickets = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketByIdQuery(10), CancellationToken.None);

        // Assert - AC-05: Internal comments are hidden for Requester
        result.Should().NotBeNull();
        result.Timeline.Should().NotContain(t => t.IsInternal);
        result.Timeline.Should().Contain(t => t.Type == TimelineItemType.Comment && t.CommentBody == "Komentar publik dari requester");
        result.Timeline.Should().NotContain(t => t.CommentBody == "Catatan internal teknisi: perlu ganti SSD");
    }

    [Fact]
    public async Task Handle_AgentAccessingTicket_ShowsInternalComments_AC05()
    {
        // Arrange - Agent has permission to see internal comments
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        var ticket = CreateSampleTicketWithTimeline();
        var tickets = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketByIdQuery(10), CancellationToken.None);

        // Assert - Agent sees both public and internal comments
        result.Should().NotBeNull();
        result.Timeline.Should().Contain(t => t.IsInternal && t.CommentBody == "Catatan internal teknisi: perlu ganti SSD");
        result.Timeline.Should().Contain(t => !t.IsInternal && t.CommentBody == "Komentar publik dari requester");
    }

    [Fact]
    public async Task Handle_Timeline_ContainsMergedCommentsAndHistorySortedByTimestamp()
    {
        // Arrange
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        var ticket = CreateSampleTicketWithTimeline();
        var tickets = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketByIdQuery(10), CancellationToken.None);

        // Assert - 2 comments + 2 history entries = 4 timeline items, chronological order
        result.Timeline.Should().HaveCount(4);
        result.Timeline.Should().BeInAscendingOrder(t => t.Timestamp);

        // First item: Created history (-2 hours)
        result.Timeline[0].Type.Should().Be(TimelineItemType.History);
        result.Timeline[0].NewValue.Should().Be("Created");

        // Last item: Internal comment (-60 minutes)
        result.Timeline[3].Type.Should().Be(TimelineItemType.Comment);
        result.Timeline[3].CommentBody.Should().Be("Catatan internal teknisi: perlu ganti SSD");
    }
}

