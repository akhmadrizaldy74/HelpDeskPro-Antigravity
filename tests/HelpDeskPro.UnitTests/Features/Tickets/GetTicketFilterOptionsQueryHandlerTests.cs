namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketFilterOptions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetTicketFilterOptionsQueryHandlerTests
{
    private readonly IApplicationDbContext _context;

    public GetTicketFilterOptionsQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task Handle_ReturnsActiveCategoriesAndItAssignees()
    {
        // Arrange
        var itDept = new Department { Id = 1, Code = "IT", Name = "IT Department" };
        var hrDept = new Department { Id = 2, Code = "HR", Name = "HR Department" };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Network", IsActive = true },
            new() { Id = 2, Name = "Hardware", IsActive = true },
            new() { Id = 3, Name = "Deprecated", IsActive = false }
        };

        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid(), FullName = "Agent IT", Department = itDept, DepartmentId = 1, IsActive = true },
            new() { Id = Guid.NewGuid(), FullName = "Requester HR", Department = hrDept, DepartmentId = 2, IsActive = true },
            new() { Id = Guid.NewGuid(), FullName = "Inactive IT", Department = itDept, DepartmentId = 1, IsActive = false }
        };

        var catSet = categories.AsTestDbSet();
        var userSet = users.AsTestDbSet();
        _context.Categories.Returns(catSet);
        _context.Users.Returns(userSet);

        var handler = new GetTicketFilterOptionsQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetTicketFilterOptionsQuery(), CancellationToken.None);

        // Assert
        result.Categories.Should().HaveCount(2);
        result.Categories.Select(c => c.Name).Should().ContainInOrder("Hardware", "Network");

        result.Assignees.Should().HaveCount(1);
        result.Assignees[0].FullName.Should().Be("Agent IT");
    }

    [Fact]
    public async Task Handle_WhenNoItAssignees_FallbacksToAnyActiveUsers()
    {
        // Arrange
        var hrDept = new Department { Id = 2, Code = "HR", Name = "HR Department" };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Software", IsActive = true }
        };

        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid(), FullName = "User 1", Department = hrDept, DepartmentId = 2, IsActive = true },
            new() { Id = Guid.NewGuid(), FullName = "User 2", Department = hrDept, DepartmentId = 2, IsActive = true }
        };

        var catSet = categories.AsTestDbSet();
        var userSet = users.AsTestDbSet();
        _context.Categories.Returns(catSet);
        _context.Users.Returns(userSet);

        var handler = new GetTicketFilterOptionsQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetTicketFilterOptionsQuery(), CancellationToken.None);

        // Assert
        result.Categories.Should().HaveCount(1);
        result.Assignees.Should().HaveCount(2);
    }
}
