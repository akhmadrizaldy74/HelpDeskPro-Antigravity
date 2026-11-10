namespace HelpDeskPro.IntegrationTests.Services;

using System.Security.Claims;
using FluentAssertions;
using HelpDeskPro.Api.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_WhenNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessor);

        // Act & Assert
        service.UserId.Should().BeNull();
        service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void UserId_WhenUserNotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor);

        // Act & Assert
        service.UserId.Should().BeNull();
        service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void UserId_WhenClaimIsGuidEmpty_ShouldReturnNull()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor);

        // Act & Assert
        service.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenValidGuidClaim_ShouldReturnUserId()
    {
        // Arrange
        var validId = Guid.NewGuid().ToString();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, validId),
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Role, "Agent")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor);

        // Act & Assert
        service.UserId.Should().Be(validId);
        service.UserName.Should().Be("John Doe");
        service.Role.Should().Be("Agent");
        service.IsAuthenticated.Should().BeTrue();
    }
}
