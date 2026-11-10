namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

public class UpdateTicketCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateTicketCommandHandler _handler;

    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly byte[] _defaultRowVersion = [1, 2, 3, 4];
    private readonly Category _catHardware = new() { Id = 1, Name = "Hardware" };
    private readonly Category _catSoftware = new() { Id = 2, Name = "Software" };

    public UpdateTicketCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        _handler = new UpdateTicketCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider);
    }

    private Ticket CreateSampleTicket(long id = 1, TicketStatus status = TicketStatus.Open)
    {
        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester" };
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
        ticket.Category = _catHardware;
        ticket.Requester = requester;
        ticket.Status = status;
        ticket.RowVersion = _defaultRowVersion;

        return ticket;
    }

    [Fact]
    public async Task Handle_RequesterUpdatesOwnOpenTicket_ShouldSucceedAndRecordHistoryOnlyForChangedFields()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.Open);
        var initialResponseDue = ticket.ResponseDueAt;
        var initialResolutionDue = ticket.ResolutionDueAt;

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware, _catSoftware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        // Update Title, Description, and Category (leave Priority the same)
        var command = new UpdateTicketCommand(
            ticket.Id,
            "Judul Baru Monitor",
            "Deskripsi baru layar bergaris",
            _catSoftware.Id,
            ticket.Priority, // same priority
            _defaultRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Judul Baru Monitor");
        result.Description.Should().Be("Deskripsi baru layar bergaris");
        result.CategoryId.Should().Be(_catSoftware.Id);
        result.CategoryName.Should().Be(_catSoftware.Name);

        // SLA dates unchanged
        ticket.ResponseDueAt.Should().Be(initialResponseDue);
        ticket.ResolutionDueAt.Should().Be(initialResolutionDue);

        // History: Title, Description, Category (3 entries, NO Priority entry)
        ticket.Histories.Should().Contain(h => h.FieldName == "Title" && h.NewValue == "Judul Baru Monitor");
        ticket.Histories.Should().Contain(h => h.FieldName == "Description" && h.NewValue == "Deskripsi baru layar bergaris");
        ticket.Histories.Should().Contain(h => h.FieldName == "Category" && h.NewValue == "Software");
        ticket.Histories.Should().NotContain(h => h.FieldName == "Priority");

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PriorityChange_DoesNotModifySlaDueDates()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.Open);
        var initialResponseDue = ticket.ResponseDueAt;
        var initialResolutionDue = ticket.ResolutionDueAt;

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        var command = new UpdateTicketCommand(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            _catHardware.Id,
            TicketPriority.Critical, // Changed priority
            _defaultRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Priority.Should().Be(TicketPriority.Critical);
        ticket.Priority.Should().Be(TicketPriority.Critical);
        ticket.ResponseDueAt.Should().Be(initialResponseDue);
        ticket.ResolutionDueAt.Should().Be(initialResolutionDue);
        ticket.Histories.Should().ContainSingle(h => h.FieldName == "Priority" && h.NewValue == "Critical");
    }

    [Fact]
    public async Task Handle_RequesterUpdatesTicketWhenInProgress_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.InProgress);

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        var command = new UpdateTicketCommand(
            ticket.Id,
            "Judul Baru",
            "Deskripsi baru",
            _catHardware.Id,
            TicketPriority.High,
            _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Requester hanya dapat mengubah tiket saat status masih Open.");
    }

    [Fact]
    public async Task Handle_AgentAttemptsToUpdateTicket_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.Open);
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Agent");

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        var command = new UpdateTicketCommand(
            ticket.Id,
            "Judul Baru",
            "Deskripsi baru",
            _catHardware.Id,
            TicketPriority.High,
            _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Agent tidak memiliki wewenang untuk mengubah detail tiket.");
    }

    [Fact]
    public async Task Handle_AdminUpdatesTicket_ShouldSucceedEvenIfStatusNotOpen()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.InProgress);
        _currentUserService.UserId.Returns(_adminId.ToString());
        _currentUserService.Role.Returns("Admin");

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        var command = new UpdateTicketCommand(
            ticket.Id,
            "Judul Diedit Admin",
            "Deskripsi diedit admin",
            _catHardware.Id,
            TicketPriority.Critical,
            _defaultRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Judul Diedit Admin");
        ticket.Title.Should().Be("Judul Diedit Admin");
    }

    [Fact]
    public async Task Handle_RowVersionMismatch_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.Open);

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var categoriesSet = new List<Category> { _catHardware }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Categories.Returns(categoriesSet);

        var staleRowVersion = new byte[] { 9, 9, 9, 9 };
        var command = new UpdateTicketCommand(
            ticket.Id,
            "Judul Baru",
            "Deskripsi",
            _catHardware.Id,
            TicketPriority.High,
            staleRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
            .WithMessage("Tiket sudah diubah pengguna lain, muat ulang halaman.");
    }
}

