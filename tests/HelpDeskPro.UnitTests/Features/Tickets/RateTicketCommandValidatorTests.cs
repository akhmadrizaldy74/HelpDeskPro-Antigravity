namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;
using Xunit;

public class RateTicketCommandValidatorTests
{
    private readonly RateTicketCommandValidator _validator = new();

    [Theory]
    [InlineData(1, 1, "Cukup")]
    [InlineData(10, 5, "Sangat baik!")]
    [InlineData(999, 3, null)]
    public void Validate_WhenValid_PassesValidation(long ticketId, int rating, string? comment)
    {
        // Arrange
        var command = new RateTicketCommand(ticketId, rating, comment);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    public void Validate_WhenTicketIdInvalid_FailsValidation(long ticketId, int rating)
    {
        // Arrange
        var command = new RateTicketCommand(ticketId, rating, "Ok");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RateTicketCommand.TicketId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-2)]
    public void Validate_WhenRatingOutOfRange_FailsValidation(int rating)
    {
        // Arrange
        var command = new RateTicketCommand(1, rating, "Ok");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RateTicketCommand.Rating));
    }

    [Fact]
    public void Validate_WhenCommentExceeds500Chars_FailsValidation()
    {
        // Arrange
        var command = new RateTicketCommand(1, 5, new string('x', 501));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RateTicketCommand.Comment));
    }
}
