namespace HelpDeskPro.UnitTests.Features.Escalations;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;
using Xunit;

public class EscalateTicketCommandValidatorTests
{
    private readonly EscalateTicketCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new EscalateTicketCommand(1, "Eskalasi membutuhkan persetujuan supervisor IT");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyNote_ShouldHaveValidationError(string? note)
    {
        // Arrange
        var command = new EscalateTicketCommand(1, note!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note" && e.ErrorMessage == "Alasan eskalasi wajib diisi.");
    }

    [Theory]
    [InlineData("123456789")] // 9 chars
    [InlineData("Pendek")]
    public void Validate_NoteTooShort_ShouldHaveValidationError(string note)
    {
        // Arrange
        var command = new EscalateTicketCommand(1, note);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note" && e.ErrorMessage == "Alasan eskalasi minimal 10 karakter.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidTicketId_ShouldHaveValidationError(long ticketId)
    {
        // Arrange
        var command = new EscalateTicketCommand(ticketId, "Alasan eskalasi valid lebih dari 10 karakter");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TicketId" && e.ErrorMessage == "ID tiket tidak valid.");
    }
}

