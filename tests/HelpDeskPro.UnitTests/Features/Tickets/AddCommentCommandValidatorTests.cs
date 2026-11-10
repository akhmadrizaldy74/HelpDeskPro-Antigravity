namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.AddComment;
using Xunit;

public class AddCommentCommandValidatorTests
{
    private readonly AddCommentCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddCommentCommand(1, "Ini komentar uji coba", false);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidTicketId_ShouldHaveValidationError(long id)
    {
        // Arrange
        var command = new AddCommentCommand(id, "Komentar", false);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TicketId" && e.ErrorMessage == "ID tiket tidak valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyBody_ShouldHaveValidationError(string? body)
    {
        // Arrange
        var command = new AddCommentCommand(1, body!, false);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body" && e.ErrorMessage == "Isi komentar tidak boleh kosong.");
    }

    [Fact]
    public void Validate_BodyTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longBody = new string('A', 4001);
        var command = new AddCommentCommand(1, longBody, false);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body" && e.ErrorMessage == "Isi komentar melebihi batas maksimal 4000 karakter.");
    }
}

