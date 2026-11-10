namespace HelpDeskPro.IntegrationTests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HelpDeskPro.Api.Controllers;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Application.Features.Master.Users.Models;
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

public class MasterControllersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _adminId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Guid _agentId = Guid.Parse("22222222-3333-4444-5555-666666666666");

    public MasterControllersIntegrationTests(WebApplicationFactory<Program> factory)
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
            new Claim(ClaimTypes.Email, "admin@helpdeskpro.local"),
            new Claim(ClaimTypes.Name, "Admin User"),
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

    private HttpClient CreateClientWithMocks(string role)
    {
        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var contextMock = Substitute.For<IApplicationDbContext>();

                var itDept = new Department { Id = 1, Code = "IT", Name = "IT Support", IsActive = true };
                var depts = new List<Department> { itDept };
                var deptSet = depts.AsTestDbSet();
                contextMock.Departments.Returns(deptSet);

                var categories = new List<Category>
                {
                    new() { Id = 1, Name = "Hardware", IsActive = true },
                    new() { Id = 2, Name = "Software", IsActive = true }
                };
                var catSet = categories.AsTestDbSet();
                contextMock.Categories.Returns(catSet);

                var adminUser = new ApplicationUser
                {
                    Id = _adminId,
                    FullName = "Admin",
                    DepartmentId = 1,
                    Department = itDept,
                    IsActive = true
                };
                var users = new List<ApplicationUser> { adminUser };
                var userSet = users.AsTestDbSet();
                contextMock.Users.Returns(userSet);

                var slaPolicies = new List<SlaPolicy>
                {
                    new() { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 }
                };
                var slaSet = slaPolicies.AsTestDbSet();
                contextMock.SlaPolicies.Returns(slaSet);

                var ticketSet = new List<Ticket>().AsTestDbSet();
                contextMock.Tickets.Returns(ticketSet);

                contextMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

                services.AddScoped(_ => contextMock);

                var store = Substitute.For<IUserStore<ApplicationUser>>();
                var userManager = Substitute.For<UserManager<ApplicationUser>>(
                    store, null, null, null, null, null, null, null, null);
                userManager.GetRolesAsync(Arg.Any<ApplicationUser>()).Returns(new List<string> { "Admin" });
                services.AddScoped(_ => userManager);
            });
        });

        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestJwt(_adminId, role));
        return client;
    }

    [Fact]
    public async Task MasterEndpoints_WhenCalledByAgent_Returns403Forbidden()
    {
        // Arrange
        var client = CreateClientWithMocks("Agent");

        // Act & Assert
        var deptRes = await client.GetAsync("/api/master/departments");
        deptRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var catRes = await client.GetAsync("/api/master/categories");
        catRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var userRes = await client.GetAsync("/api/master/users");
        userRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var slaRes = await client.GetAsync("/api/master/sla-policies");
        slaRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MasterDepartments_GetAllAndCreate_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMocks("Admin");

        // Act - Get
        var getRes = await client.GetAsync("/api/master/departments");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>();
        getResult!.Data.Should().NotBeNull();

        // Act - Post
        var postRes = await client.PostAsJsonAsync("/api/master/departments", new CreateDepartmentCommand("HR", "Human Resources"));
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MasterCategories_GetAllAndCreate_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMocks("Admin");

        // Act - Get
        var getRes = await client.GetAsync("/api/master/categories");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Post
        var postRes = await client.PostAsJsonAsync("/api/master/categories", new CreateCategoryCommand("Network"));
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MasterUsers_GetUsers_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMocks("Admin");

        // Act
        var res = await client.GetAsync("/api/master/users?page=1&pageSize=10");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserListItemDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MasterSlaPolicies_GetAllAndUpdate_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMocks("Admin");

        // Act - Get
        var getRes = await client.GetAsync("/api/master/sla-policies");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Put
        var updateReq = new UpdateSlaPolicyRequest(60, 300);
        var putRes = await client.PutAsJsonAsync($"/api/master/sla-policies/{TicketPriority.High}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var putResult = await putRes.Content.ReadFromJsonAsync<ApiResponse<SlaPolicyDto>>();
        putResult!.Data!.ResponseTimeMinutes.Should().Be(60);
        putResult.Data.ResolutionTimeMinutes.Should().Be(300);
    }
}

