namespace HelpDeskPro.IntegrationTests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

public class DashboardControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _supervisorId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Guid _requesterId = Guid.Parse("22222222-3333-4444-5555-666666666666");

    public DashboardControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    private static string GenerateTestJwt(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASecretKeyForHelpDeskProSecuritySuperSecret2026!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@helpdeskpro.local"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "HelpDeskPro",
            audience: "HelpDeskProClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient CreateClientWithMocks(string role, Guid userId)
    {
        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var contextMock = Substitute.For<IApplicationDbContext>();

                var itDept = new Department { Id = 1, Code = "IT", Name = "IT Support", IsActive = true };
                var deptSet = new List<Department> { itDept }.AsTestDbSet();
                contextMock.Departments.Returns(deptSet);

                var supervisorUser = new ApplicationUser
                {
                    Id = _supervisorId,
                    FullName = "Supervisor IT",
                    DepartmentId = 1,
                    Department = itDept,
                    IsActive = true
                };

                var userSet = new List<ApplicationUser> { supervisorUser }.AsTestDbSet();
                contextMock.Users.Returns(userSet);

                var now = DateTime.UtcNow;
                var tickets = new List<Ticket>
                {
                    new()
                    {
                        Id = 1,
                        TicketNo = "HD-2026-000001",
                        Title = "Sample Open Ticket",
                        Status = TicketStatus.Open,
                        CreatedAt = now.AddDays(-2),
                        ResponseDueAt = now.AddHours(2),
                        ResolutionDueAt = now.AddHours(10)
                    }
                };

                var ticketSet = tickets.AsTestDbSet();
                contextMock.Tickets.Returns(ticketSet);

                var escalationSet = new List<Escalation>().AsTestDbSet();
                contextMock.Escalations.Returns(escalationSet);

                var holidaysSet = new List<Holiday>().AsTestDbSet();
                contextMock.Holidays.Returns(holidaysSet);

                contextMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

                services.AddScoped(_ => contextMock);

                // Mock UserManager for Agent query
                var store = Substitute.For<IUserStore<ApplicationUser>>();
                var userManager = Substitute.For<UserManager<ApplicationUser>>(
                    store, null, null, null, null, null, null, null, null);
                userManager.GetUsersInRoleAsync("Agent")
                    .Returns(new List<ApplicationUser>
                    {
                        new() { Id = Guid.NewGuid(), FullName = "Agent One", Email = "agent@test.local", DepartmentId = 1, IsActive = true }
                    });
                services.AddScoped(_ => userManager);
            });
        });

        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestJwt(userId, role));
        return client;
    }

    [Fact]
    public async Task GetSummary_RequesterRole_Returns403Forbidden()
    {
        // Arrange
        var client = CreateClientWithMocks("Requester", _requesterId);

        // Act
        var response = await client.GetAsync("/api/dashboard/summary?periodDays=30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_SupervisorRole_Returns200OkWithSummary()
    {
        // Arrange
        var client = CreateClientWithMocks("Supervisor", _supervisorId);

        // Act
        var response = await client.GetAsync("/api/dashboard/summary?periodDays=30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PeriodDays.Should().Be(30);
    }

    [Fact]
    public async Task GetAgentWorkload_RequesterRole_Returns403Forbidden()
    {
        // Arrange
        var client = CreateClientWithMocks("Requester", _requesterId);

        // Act
        var response = await client.GetAsync("/api/dashboard/agent-workload");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAgentWorkload_SupervisorRole_Returns200Ok()
    {
        // Arrange
        var client = CreateClientWithMocks("Supervisor", _supervisorId);

        // Act
        var response = await client.GetAsync("/api/dashboard/agent-workload");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AgentWorkloadDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPendingEscalations_SupervisorRole_Returns200Ok()
    {
        // Arrange
        var client = CreateClientWithMocks("Supervisor", _supervisorId);

        // Act
        var response = await client.GetAsync("/api/dashboard/pending-escalations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PendingEscalationDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}

