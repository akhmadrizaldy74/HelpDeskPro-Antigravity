namespace HelpDeskPro.UnitTests.Features.Auth;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Auth.Commands.Login;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public class LoginCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        _tokenService = Substitute.For<ITokenService>();
        _context = Substitute.For<IApplicationDbContext>();

        _handler = new LoginCommandHandler(_userManager, _tokenService, _context);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnTokenAndUserDto()
    {
        // Arrange
        const string email = "admin@helpdeskpro.local";
        const string password = "Password123!";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Budi Santoso",
            Department = new Department { Name = "IT Department" }
        };

        var usersSet = new[] { user }.AsTestDbSet();
        _context.Users.Returns(usersSet);
        _userManager.IsLockedOutAsync(user).Returns(false);
        _userManager.CheckPasswordAsync(user, password).Returns(true);
        _userManager.GetRolesAsync(user).Returns(new[] { "Admin" });

        var expectedExpiresAt = DateTime.UtcNow.AddHours(8);
        _tokenService.GenerateToken(user, Arg.Any<IList<string>>(), "IT Department")
            .Returns(("dummy-jwt-token", expectedExpiresAt));

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("dummy-jwt-token");
        result.User.FullName.Should().Be("Budi Santoso");
        result.User.Role.Should().Be("Admin");
        result.User.DepartmentName.Should().Be("IT Department");

        await _userManager.Received(1).ResetAccessFailedCountAsync(user);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldIncrementAccessFailedCountAndThrowBusinessRuleException()
    {
        // Arrange
        const string email = "admin@helpdeskpro.local";
        const string password = "WrongPassword";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Budi Santoso"
        };

        var usersSet = new[] { user }.AsTestDbSet();
        _context.Users.Returns(usersSet);
        _userManager.IsLockedOutAsync(user).Returns(false);
        _userManager.CheckPasswordAsync(user, password).Returns(false);

        var command = new LoginCommand(email, password);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Email atau password yang Anda masukkan salah*");

        await _userManager.Received(1).AccessFailedAsync(user);
    }

    [Fact]
    public async Task Handle_WhenAccountIsLockedOut_ShouldThrowBusinessRuleException()
    {
        // Arrange
        const string email = "admin@helpdeskpro.local";
        const string password = "AnyPassword";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Budi Santoso"
        };

        var usersSet = new[] { user }.AsTestDbSet();
        _context.Users.Returns(usersSet);
        _userManager.IsLockedOutAsync(user).Returns(true);

        var command = new LoginCommand(email, password);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*terkunci*");

        await _userManager.DidNotReceive().CheckPasswordAsync(user, Arg.Any<string>());
    }
}
