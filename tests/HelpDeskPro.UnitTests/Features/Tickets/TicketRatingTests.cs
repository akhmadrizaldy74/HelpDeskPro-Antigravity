namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using Xunit;

public class TicketRatingTests
{
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc);

    private Ticket CreateResolvedTicket()
    {
        return new Ticket
        {
            Id = 1,
            TicketNo = "HD-2026-000001",
            Title = "Printer tidak berfungsi",
            Status = TicketStatus.Resolved,
            RequesterId = _requesterId,
            ResolvedAt = _utcNow.AddHours(-1)
        };
    }

    [Fact]
    public void Rate_WhenResolvedAndByRequester_SetsRatingCommentAndClosesTicket()
    {
        // Arrange
        var ticket = CreateResolvedTicket();

        // Act
        ticket.Rate(5, "Pelayanan teknisi sangat cepat dan ramah!", _requesterId, "Requester", _utcNow);

        // Assert
        ticket.Rating.Should().Be(5);
        ticket.RatingComment.Should().Be("Pelayanan teknisi sangat cepat dan ramah!");
        ticket.RatedAt.Should().Be(_utcNow);
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(_utcNow);

        ticket.Histories.Should().Contain(h => h.FieldName == "Rating" && h.NewValue == "5/5");
        ticket.Histories.Should().Contain(h => h.FieldName == "Status" && h.OldValue == "Resolved" && h.NewValue == "Closed");
    }

    [Fact]
    public void Rate_WhenTicketNotResolved_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.InProgress,
            RequesterId = _requesterId
        };

        // Act
        var act = () => ticket.Rate(5, "Ok", _requesterId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Rating hanya dapat diberikan pada tiket yang berstatus Resolved*");
    }

    [Fact]
    public void Rate_WhenNotRequesterOwner_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateResolvedTicket();

        // Act
        var act = () => ticket.Rate(4, "Bukan pemohon", _otherUserId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Hanya pemohon tiket yang dapat memberikan rating kepuasan*");
    }

    [Fact]
    public void Rate_WhenAlreadyRated_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateResolvedTicket();
        ticket.Rate(4, "Rating pertama", _requesterId, "Requester", _utcNow);
        ticket.Status = TicketStatus.Resolved; // Reset status artificially to test idempotency guard

        // Act
        var act = () => ticket.Rate(5, "Rating kedua", _requesterId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Tiket ini sudah pernah diberi rating kepuasan*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rate_WhenRatingOutOfRange_ThrowsBusinessRuleException(int invalidRating)
    {
        // Arrange
        var ticket = CreateResolvedTicket();

        // Act
        var act = () => ticket.Rate(invalidRating, "Nilai tidak valid", _requesterId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Rating kepuasan harus bernilai antara 1 dan 5*");
    }

    [Fact]
    public void Rate_WhenCommentExceeds500Chars_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateResolvedTicket();
        var longComment = new string('A', 501);

        // Act
        var act = () => ticket.Rate(5, longComment, _requesterId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Komentar rating tidak boleh melebihi 500 karakter*");
    }
}
