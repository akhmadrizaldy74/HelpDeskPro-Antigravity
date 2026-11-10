namespace HelpDeskPro.UnitTests.Common.Models;

using FluentAssertions;
using HelpDeskPro.Application.Common.Models;
using Xunit;

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Ok_SetsSuccessAndTraceId()
    {
        // Act
        var response = ApiResponse.Ok("SampleData", "trace-123");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be("SampleData");
        response.TraceId.Should().Be("trace-123");
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApiResponse_Failure_SetsFailureAndErrors()
    {
        // Act
        var response = ApiResponse.Failure<string>(["Error 1", "Error 2"], "trace-456");

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.TraceId.Should().Be("trace-456");
        response.Errors.Should().ContainInOrder("Error 1", "Error 2");
    }

    [Fact]
    public void PagedResult_Create_CalculatesTotalPages()
    {
        // Act
        var paged = PagedResult.Create(new List<int> { 1, 2, 3 }, page: 1, pageSize: 2, totalCount: 5);

        // Assert
        paged.Items.Should().HaveCount(3);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.TotalCount.Should().Be(5);
        paged.TotalPages.Should().Be(3);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeFalse();
    }
}
