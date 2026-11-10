namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class UploadTicketAttachmentCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly UploadTicketAttachmentCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public UploadTicketAttachmentCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _fileStorage = Substitute.For<IFileStorage>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_userId.ToString());
        _currentUserService.Role.Returns("Requester");

        _handler = new UploadTicketAttachmentCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
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
    public async Task Handle_ValidUpload_SavesFileAndAddsAttachment()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        _fileStorage.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("guid-123.pdf");

        using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadTicketAttachmentCommand(ticket.Id, stream, "laporan.pdf", "application/pdf", 1024);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("laporan.pdf");
        result.ContentType.Should().Be("application/pdf");
        ticket.Attachments.Should().ContainSingle(a => a.FileName == "laporan.pdf" && a.StoragePath == "guid-123.pdf");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTicketAlreadyHas5Attachments_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        for (var i = 1; i <= 5; i++)
        {
            ticket.Attachments.Add(new TicketAttachment
            {
                Id = i,
                FileName = $"file{i}.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 100,
                StoragePath = $"path{i}.pdf",
                UploadedById = _userId,
                UploadedAt = _utcNow
            });
        }

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadTicketAttachmentCommand(ticket.Id, stream, "file6.pdf", "application/pdf", 1024);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Jumlah lampiran untuk tiket ini sudah mencapai batas maksimal (5 file).");
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ticketsSet = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadTicketAttachmentCommand(999, stream, "file.pdf", "application/pdf", 1024);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

