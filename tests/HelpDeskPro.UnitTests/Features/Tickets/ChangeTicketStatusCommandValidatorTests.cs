namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;
using HelpDeskPro.Domain.Enums;
using Xunit;

public class ChangeTicketStatusCommandValidatorTests
{
    private readonly ChangeTicketStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(1, TicketStatus.InProgress, null, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidId_ShouldHaveValidationError(long id)
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(id, TicketStatus.InProgress, null, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id" && e.ErrorMessage == "ID tiket tidak valid.");
    }

    [Fact]
    public void Validate_InvalidStatusEnum_ShouldHaveValidationError()
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(1, (TicketStatus)99, null, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status" && e.ErrorMessage == "Status tiket tidak valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_PendingWithoutReason_ShouldHaveValidationError(string? reason)
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(1, TicketStatus.Pending, reason, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage == "Alasan penundaan tiket wajib diisi.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ResolvedWithoutReason_ShouldHaveValidationError(string? reason)
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(1, TicketStatus.Resolved, reason, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage == "Catatan penyelesaian tiket wajib diisi.");
    }

    [Fact]
    public void Validate_EmptyRowVersion_ShouldHaveValidationError()
    {
        // Arrange
        var command = new ChangeTicketStatusCommand(1, TicketStatus.InProgress, null, Array.Empty<byte>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RowVersion" && e.ErrorMessage == "RowVersion wajib disertakan.");
    }
}

