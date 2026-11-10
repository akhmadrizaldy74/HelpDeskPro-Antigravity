namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Queries.DownloadTicketAttachment;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class DownloadTicketAttachmentQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly DownloadTicketAttachmentQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public DownloadTicketAttachmentQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _fileStorage = Substitute.For<IFileStorage>();

        _currentUserService.UserId.Returns(_userId.ToString());
        _currentUserService.Role.Returns("Requester");

        _handler = new DownloadTicketAttachmentQueryHandler(
            _context,
            _currentUserService,
            _fileStorage);
    }

    private Ticket CreateSampleTicket(long id = 1)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _userId, FullName = "Budi Requester" };
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
    public async Task Handle_ValidRequest_ReturnsAttachmentStream()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var attachment = new TicketAttachment
        {
            Id = 10,
            TicketId = ticket.Id,
            FileName = "foto.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            StoragePath = "guid-123.jpg",
            UploadedById = _userId,
            UploadedAt = _utcNow
        };
        ticket.Attachments.Add(attachment);

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var fakeStream = new MemoryStream([1, 2, 3]);
        _fileStorage.GetFileAsync("guid-123.jpg", Arg.Any<CancellationToken>())
            .Returns(fakeStream);

        var query = new DownloadTicketAttachmentQuery(ticket.Id, attachment.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("foto.jpg");
        result.ContentType.Should().Be("image/jpeg");
        result.Stream.Should().BeSameAs(fakeStream);
    }

    [Fact]
    public async Task Handle_AttachmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var query = new DownloadTicketAttachmentQuery(ticket.Id, 999);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PhysicalFileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var attachment = new TicketAttachment
        {
            Id = 11,
            TicketId = ticket.Id,
            FileName = "foto.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            StoragePath = "missing.jpg",
            UploadedById = _userId,
            UploadedAt = _utcNow
        };
        ticket.Attachments.Add(attachment);

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        _fileStorage.GetFileAsync("missing.jpg", Arg.Any<CancellationToken>())
            .Returns((Stream?)null);

        var query = new DownloadTicketAttachmentQuery(ticket.Id, attachment.Id);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

