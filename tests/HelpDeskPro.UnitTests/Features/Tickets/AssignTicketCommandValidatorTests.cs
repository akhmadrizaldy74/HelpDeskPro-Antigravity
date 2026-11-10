namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;
using Xunit;

public class AssignTicketCommandValidatorTests
{
    private readonly AssignTicketCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignTicketCommand(1, Guid.NewGuid(), new byte[] { 1, 2, 3 });

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
        var command = new AssignTicketCommand(id, Guid.NewGuid(), new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id" && e.ErrorMessage == "ID tiket tidak valid.");
    }

    [Fact]
    public void Validate_EmptyAssigneeId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new AssignTicketCommand(1, Guid.Empty, new byte[] { 1, 2, 3 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssigneeId" && e.ErrorMessage == "Assignee ID wajib diisi.");
    }

    [Fact]
    public void Validate_EmptyRowVersion_ShouldHaveValidationError()
    {
        // Arrange
        var command = new AssignTicketCommand(1, Guid.NewGuid(), Array.Empty<byte>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RowVersion" && e.ErrorMessage == "RowVersion wajib disertakan.");
    }
}
