namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using FluentValidation.TestHelper;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;
using Xunit;

public class GetTicketsQueryValidatorTests
{
    private readonly GetTicketsQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsLessThanOne_ShouldHaveValidationError(int page)
    {
        // Arrange
        var query = new GetTicketsQuery { Page = page };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Page)
              .WithErrorMessage("Nomor halaman minimal 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsOutOfRange_ShouldHaveValidationError(int pageSize)
    {
        // Arrange
        var query = new GetTicketsQuery { PageSize = pageSize };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.PageSize)
              .WithErrorMessage("Ukuran halaman harus antara 1 dan 100.");
    }

    [Fact]
    public void Validate_WhenDateToIsEarlierThanDateFrom_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GetTicketsQuery
        {
            DateFrom = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.DateTo)
              .WithErrorMessage("Tanggal akhir harus lebih besar atau sama dengan tanggal awal.");
    }

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new GetTicketsQuery
        {
            Page = 1,
            PageSize = 20,
            DateFrom = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

