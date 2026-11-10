namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;
using HelpDeskPro.Domain.Enums;
using Xunit;

public class UpdateTicketCommandValidatorTests
{
    private readonly UpdateTicketCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateTicketCommand(1, "Judul Valid Tiket", "Deskripsi tiket", 1, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTitle_ShouldHaveValidationError(string? title)
    {
        // Arrange
        var command = new UpdateTicketCommand(1, title!, "Deskripsi", 1, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "Judul tiket tidak boleh kosong.");
    }

    [Theory]
    [InlineData("ABCD")] // 4 characters
    public void Validate_TitleTooShort_ShouldHaveValidationError(string title)
    {
        // Arrange
        var command = new UpdateTicketCommand(1, title, "Deskripsi", 1, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "Judul tiket harus antara 5 dan 200 karakter.");
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longTitle = new string('A', 201);
        var command = new UpdateTicketCommand(1, longTitle, "Deskripsi", 1, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "Judul tiket harus antara 5 dan 200 karakter.");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longDesc = new string('A', 4001);
        var command = new UpdateTicketCommand(1, "Judul Tiket", longDesc, 1, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage == "Deskripsi tiket melebihi batas maksimal 4000 karakter.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidCategoryId_ShouldHaveValidationError(int categoryId)
    {
        // Arrange
        var command = new UpdateTicketCommand(1, "Judul Tiket", "Deskripsi", categoryId, TicketPriority.High, [1, 2, 3]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId" && e.ErrorMessage == "Kategori wajib dipilih.");
    }

    [Fact]
    public void Validate_EmptyRowVersion_ShouldHaveValidationError()
    {
        // Arrange
        var command = new UpdateTicketCommand(1, "Judul Tiket", "Deskripsi", 1, TicketPriority.High, Array.Empty<byte>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RowVersion" && e.ErrorMessage == "RowVersion wajib disertakan.");
    }
}

