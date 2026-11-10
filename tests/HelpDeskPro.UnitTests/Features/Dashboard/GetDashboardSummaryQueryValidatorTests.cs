namespace HelpDeskPro.UnitTests.Features.Dashboard;

using FluentAssertions;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using Xunit;

public class GetDashboardSummaryQueryValidatorTests
{
    private readonly GetDashboardSummaryQueryValidator _validator = new();

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    public void Validate_ValidPeriod_PassesValidation(int periodDays)
    {
        // Act
        var result = _validator.Validate(new GetDashboardSummaryQuery(periodDays));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(60)]
    [InlineData(-5)]
    public void Validate_InvalidPeriod_FailsValidation(int periodDays)
    {
        // Act
        var result = _validator.Validate(new GetDashboardSummaryQuery(periodDays));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetDashboardSummaryQuery.PeriodDays));
    }
}

