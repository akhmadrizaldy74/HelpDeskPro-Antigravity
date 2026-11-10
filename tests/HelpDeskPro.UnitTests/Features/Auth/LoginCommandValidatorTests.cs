namespace HelpDeskPro.UnitTests.Features.Auth;

using FluentValidation.TestHelper;
using HelpDeskPro.Application.Features.Auth.Commands.Login;
using Xunit;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenEmailIsEmpty_ShouldHaveValidationError(string? email)
    {
        // Arrange
        var command = new LoginCommand(email!, "Password123!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Email)
              .WithErrorMessage("Email wajib diisi.");
    }

    [Fact]
    public void Validate_WhenEmailIsInvalidFormat_ShouldHaveValidationError()
    {
        // Arrange
        var command = new LoginCommand("bukan-email", "Password123!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Email)
              .WithErrorMessage("Format email tidak valid.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenPasswordIsEmpty_ShouldHaveValidationError(string? password)
    {
        // Arrange
        var command = new LoginCommand("test@helpdeskpro.local", password!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Password)
              .WithErrorMessage("Password wajib diisi.");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new LoginCommand("test@helpdeskpro.local", "Password123!");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

