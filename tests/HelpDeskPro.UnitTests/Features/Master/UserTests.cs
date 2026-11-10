namespace HelpDeskPro.UnitTests.Features.Master;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.DeleteUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.ResetUserPassword;
using HelpDeskPro.Application.Features.Master.Users.Commands.ToggleUserStatus;
using HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;
using HelpDeskPro.Application.Features.Master.Users.Queries.GetUserById;
using HelpDeskPro.Application.Features.Master.Users.Queries.GetUsers;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public class UserTests
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserTests()
    {
        _context = Substitute.For<IApplicationDbContext>();

        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task GetUsers_ReturnsPagedUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP001", FullName = "User 1", Email = "u1@test.local", DepartmentId = 1, IsActive = true },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP002", FullName = "User 2", Email = "u2@test.local", DepartmentId = 1, IsActive = true }
        };

        var userSet = users.AsTestDbSet();
        _context.Users.Returns(userSet);
        _userManager.GetRolesAsync(Arg.Any<ApplicationUser>()).Returns(new List<string> { "Agent" });

        var handler = new GetUsersQueryHandler(_context, _userManager);

        // Act
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateUser_WhenValid_CreatesAndAssignsRole()
    {
        // Arrange
        var emptyUserSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Users.Returns(emptyUserSet);
        var depts = new List<Department> { new() { Id = 1, Name = "IT" } };
        var deptSet = depts.AsTestDbSet();
        _context.Departments.Returns(deptSet);

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        var handler = new CreateUserCommandHandler(_context, _userManager);

        // Act
        var command = new CreateUserCommand("EMP100", "Budi Baru", "budi@test.local", "P@ss123", 1, "Agent");
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.FullName.Should().Be("Budi Baru");
        result.Role.Should().Be("Agent");
        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), "P@ss123");
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<ApplicationUser>(), "Agent");
    }

    [Fact]
    public async Task UpdateUser_WhenSupervisorIsSelf_ThrowsBusinessRuleException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FullName = "User Self", DepartmentId = 1 };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        var deptSet = new List<Department> { new() { Id = 1, Name = "IT" } }.AsTestDbSet();
        _context.Users.Returns(userSet);
        _context.Departments.Returns(deptSet);

        var handler = new UpdateUserCommandHandler(_context, _userManager);

        // Act
        var command = new UpdateUserCommand(userId, "User Self", 1, "Agent", userId, true);
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Pengguna tidak dapat menjadi supervisor bagi dirinya sendiri*");
    }

    [Fact]
    public async Task ResetPassword_WhenValid_CallsResetPasswordAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FullName = "Reset Me" };

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("valid-token");
        _userManager.ResetPasswordAsync(user, "valid-token", "NewPass123!").Returns(IdentityResult.Success);

        var handler = new ResetUserPasswordCommandHandler(_userManager);

        // Act
        await handler.Handle(new ResetUserPasswordCommand(userId, "NewPass123!"), CancellationToken.None);

        // Assert
        await _userManager.Received(1).ResetPasswordAsync(user, "valid-token", "NewPass123!");
    }

    [Fact]
    public async Task ToggleStatus_WhenDeactivated_SetsLockoutEnd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FullName = "Active User", IsActive = true, Department = new Department { Name = "IT" } };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        _context.Users.Returns(userSet);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "Requester" });

        var handler = new ToggleUserStatusCommandHandler(_context, _userManager);

        // Act
        var result = await handler.Handle(new ToggleUserStatusCommand(userId, false), CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
        user.IsActive.Should().BeFalse();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUser_WhenHasTickets_ThrowsBusinessRuleException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FullName = "Has Tickets" };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        var ticketSet = new List<Ticket> { new() { Id = 1, RequesterId = userId } }.AsTestDbSet();

        _context.Users.Returns(userSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new DeleteUserCommandHandler(_context, _userManager);

        // Act
        var act = async () => await handler.Handle(new DeleteUserCommand(userId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Pengguna masih memiliki riwayat tiket*");
    }

    [Fact]
    public async Task GetUserById_WhenFound_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            EmployeeNo = "EMP001",
            FullName = "John Doe",
            Email = "john@test.local",
            DepartmentId = 1,
            Department = new Department { Id = 1, Name = "IT" },
            IsActive = true
        };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        _context.Users.Returns(userSet);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "Agent" });

        var handler = new GetUserByIdQueryHandler(_context, _userManager);

        // Act
        var result = await handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        // Assert
        result.Id.Should().Be(userId);
        result.FullName.Should().Be("John Doe");
        result.Role.Should().Be("Agent");
        result.DepartmentName.Should().Be("IT");
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Users.Returns(userSet);

        var handler = new GetUserByIdQueryHandler(_context, _userManager);

        // Act
        var act = async () => await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_WhenValid_UpdatesRoleAndDepartment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            EmployeeNo = "EMP001",
            FullName = "Old Name",
            DepartmentId = 1,
            IsActive = true
        };
        var dept2 = new Department { Id = 2, Name = "Finance" };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        var deptSet = new List<Department> { dept2 }.AsTestDbSet();
        _context.Users.Returns(userSet);
        _context.Departments.Returns(deptSet);

        _userManager.GetRolesAsync(user).Returns(new List<string> { "Requester" });
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(user, "Agent").Returns(IdentityResult.Success);

        var handler = new UpdateUserCommandHandler(_context, _userManager);

        // Act
        var command = new UpdateUserCommand(userId, "New Name", 2, "Agent", null, true);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.FullName.Should().Be("New Name");
        result.DepartmentId.Should().Be(2);
        result.Role.Should().Be("Agent");
        await _userManager.Received(1).AddToRoleAsync(user, "Agent");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUser_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Users.Returns(userSet);

        var handler = new UpdateUserCommandHandler(_context, _userManager);

        // Act
        var act = async () => await handler.Handle(new UpdateUserCommand(Guid.NewGuid(), "Any", 1, "Agent", null, true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_DepartmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FullName = "User 1", DepartmentId = 1 };

        var userSet = new List<ApplicationUser> { user }.AsTestDbSet();
        var deptSet = new List<Department>().AsTestDbSet(); // No department
        _context.Users.Returns(userSet);
        _context.Departments.Returns(deptSet);

        var handler = new UpdateUserCommandHandler(_context, _userManager);

        // Act
        var act = async () => await handler.Handle(new UpdateUserCommand(userId, "User 1", 999, "Agent", null, true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ToggleStatus_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Users.Returns(userSet);

        var handler = new ToggleUserStatusCommandHandler(_context, _userManager);

        // Act
        var act = async () => await handler.Handle(new ToggleUserStatusCommand(Guid.NewGuid(), true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResetPassword_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var handler = new ResetUserPasswordCommandHandler(_userManager);

        // Act
        var act = async () => await handler.Handle(new ResetUserPasswordCommand(userId, "NewPass123!"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
