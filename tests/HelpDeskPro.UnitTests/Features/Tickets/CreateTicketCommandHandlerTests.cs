namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class CreateTicketCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ITicketNumberGenerator _generator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CreateTicketCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public CreateTicketCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _generator = Substitute.For<ITicketNumberGenerator>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_userId.ToString());
        _currentUserService.UserName.Returns("requester@helpdeskpro.local");

        _handler = new CreateTicketCommandHandler(
            _context,
            _generator,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>().AsTestDbSet();
        _context.Categories.Returns(categories);

        var command = new CreateTicketCommand("Laptop Rusak", "Layar pecah", 999, TicketPriority.High);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Kategori*");
    }

    [Fact]
    public async Task Handle_WhenSlaPolicyNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Hardware", IsActive = true }
        }.AsTestDbSet();
        _context.Categories.Returns(categories);

        var slaPolicies = new List<SlaPolicy>().AsTestDbSet();
        _context.SlaPolicies.Returns(slaPolicies);

        var command = new CreateTicketCommand("Laptop Rusak", "Layar pecah", 1, TicketPriority.High);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*SLA Policy*");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Hardware", IsActive = true }
        }.AsTestDbSet();
        _context.Categories.Returns(categories);

        var slaPolicies = new List<SlaPolicy>
        {
            new() { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 }
        }.AsTestDbSet();
        _context.SlaPolicies.Returns(slaPolicies);

        _currentUserService.UserId.Returns((string?)null);

        var command = new CreateTicketCommand("Laptop Rusak", "Layar pecah", 1, TicketPriority.High);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*User pembuat tiket tidak valid*");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldCreateTicketAndReturnDto()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Hardware", IsActive = true }
        }.AsTestDbSet();
        _context.Categories.Returns(categories);

        var slaPolicies = new List<SlaPolicy>
        {
            new() { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 }
        }.AsTestDbSet();
        _context.SlaPolicies.Returns(slaPolicies);

        var users = new List<ApplicationUser>
        {
            new() { Id = _userId, FullName = "Budi Requester" }
        }.AsTestDbSet();
        _context.Users.Returns(users);

        var tickets = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        _generator.GenerateTicketNumberAsync(Arg.Any<CancellationToken>())
            .Returns("HD-2026-000001");

        var command = new CreateTicketCommand("Laptop Rusak", "Layar pecah", 1, TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TicketNo.Should().Be("HD-2026-000001");
        result.Title.Should().Be("Laptop Rusak");
        result.CategoryId.Should().Be(1);
        result.CategoryName.Should().Be("Hardware");
        result.Priority.Should().Be(TicketPriority.High);
        result.Status.Should().Be(TicketStatus.Open);
        result.RequesterId.Should().Be(_userId);
        result.RequesterName.Should().Be("Budi Requester");
        result.ResponseDueAt.Should().Be(_utcNow.AddMinutes(120));
        result.ResolutionDueAt.Should().Be(_utcNow.AddMinutes(480));

        _context.Tickets.Received(1).Add(Arg.Is<Ticket>(t => t.TicketNo == "HD-2026-000001"));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

