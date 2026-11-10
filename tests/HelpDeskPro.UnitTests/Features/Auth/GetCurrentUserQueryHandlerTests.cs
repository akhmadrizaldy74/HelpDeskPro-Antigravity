namespace HelpDeskPro.UnitTests.Features.Auth;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Auth.Queries.GetCurrentUser;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public class GetCurrentUserQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GetCurrentUserQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetCurrentUserQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(userStore, null, null, null, null, null, null, null, null);

        _handler = new GetCurrentUserQueryHandler(_currentUserService, _userManager, _context);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);

        // Act
        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFoundInDb_ShouldThrowNotFoundException()
    {
        // Arrange
        _currentUserService.UserId.Returns(_userId.ToString());
        var users = new List<ApplicationUser>().AsTestDbSet();
        _context.Users.Returns(users);

        // Act
        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserFound_ShouldReturnUserDto()
    {
        // Arrange
        _currentUserService.UserId.Returns(_userId.ToString());
        var user = new ApplicationUser
        {
            Id = _userId,
            EmployeeNo = "EMP001",
            FullName = "Ahmad Requester",
            Email = "ahmad@helpdeskpro.local",
            Department = new Department { Name = "Finance" }
        };

        var users = new List<ApplicationUser> { user }.AsTestDbSet();
        _context.Users.Returns(users);

        _userManager.GetRolesAsync(user).Returns(new List<string> { "Requester" });

        // Act
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_userId);
        result.FullName.Should().Be("Ahmad Requester");
        result.DepartmentName.Should().Be("Finance");
        result.Role.Should().Be("Requester");
    }
}
