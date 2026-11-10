namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using FluentValidation.TestHelper;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Domain.Enums;
using Xunit;

public class CreateTicketCommandValidatorTests
{
    private readonly CreateTicketCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenTitleIsEmpty_ShouldHaveValidationError(string? title)
    {
        // Arrange
        var command = new CreateTicketCommand(title!, "Deskripsi", 1, TicketPriority.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Title)
              .WithErrorMessage("Judul tiket wajib diisi.");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcd")]
    public void Validate_WhenTitleIsLessThan5Characters_ShouldHaveValidationError(string title)
    {
        // Arrange
        var command = new CreateTicketCommand(title, "Deskripsi", 1, TicketPriority.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Title)
              .WithErrorMessage("Judul tiket minimal 5 karakter.");
    }

    [Fact]
    public void Validate_WhenTitleIsGreaterThan200Characters_ShouldHaveValidationError()
    {
        // Arrange
        var longTitle = new string('A', 201);
        var command = new CreateTicketCommand(longTitle, "Deskripsi", 1, TicketPriority.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Title)
              .WithErrorMessage("Judul tiket maksimal 200 karakter.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenCategoryIdIsZeroOrNegative_ShouldHaveValidationError(int categoryId)
    {
        // Arrange
        var command = new CreateTicketCommand("Laptop Rusak", "Deskripsi", categoryId, TicketPriority.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CategoryId)
              .WithErrorMessage("Kategori tiket wajib dipilih.");
    }

    [Fact]
    public void Validate_WhenPriorityIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTicketCommand("Laptop Rusak", "Deskripsi", 1, (TicketPriority)99);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Priority)
              .WithErrorMessage("Prioritas tiket tidak valid.");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new CreateTicketCommand("Monitor tidak menyala", "Layar hitam total", 1, TicketPriority.High);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

