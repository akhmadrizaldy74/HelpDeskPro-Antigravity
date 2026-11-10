namespace HelpDeskPro.IntegrationTests.Controllers;

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Infrastructure.Services;
using HelpDeskPro.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

public class TicketsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _testUserId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Guid _agentUserId = Guid.Parse("22222222-3333-4444-5555-666666666666");
    private readonly Guid _otherUserId = Guid.Parse("33333333-4444-5555-6666-777777777777");

    public TicketsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    private static string GenerateTestJwt(Guid userId, string role = "Requester")
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

    private HttpClient CreateCustomClient(
        ITicketNumberGenerator? customGenerator = null,
        List<Ticket>? seedTickets = null,
        Guid? userId = null,
        string role = "Requester",
        IFileStorage? customFileStorage = null,
        List<Notification>? seedNotifications = null,
        List<Escalation>? seedEscalations = null)
    {
        customGenerator ??= new TestAtomicTicketNumberGenerator(2026);
        var effectiveUserId = userId ?? _testUserId;

        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var contextMock = Substitute.For<IApplicationDbContext>();

                var categories = new List<Category>
                {
                    new() { Id = 1, Name = "Hardware", IsActive = true },
                    new() { Id = 2, Name = "Software", IsActive = true }
                }.AsTestDbSet();
                contextMock.Categories.Returns(categories);

                var slaPolicies = new List<SlaPolicy>
                {
                    new() { Id = 1, Priority = TicketPriority.Critical, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 240 },
                    new() { Id = 2, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 },
                    new() { Id = 3, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 },
                    new() { Id = 4, Priority = TicketPriority.Low, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 4320 }
                }.AsTestDbSet();
                contextMock.SlaPolicies.Returns(slaPolicies);

                var supervisorId = Guid.Parse("44444444-5555-6666-7777-888888888888");
                var itDept = new Department { Id = 3, Code = "IT", Name = "IT", IsActive = true };
                var users = new List<ApplicationUser>
                {
                    new() { Id = _testUserId, FullName = "Budi Requester", Email = "requester@helpdeskpro.local", DepartmentId = 1 },
                    new() { Id = _agentUserId, FullName = "Doni Agent", Email = "agent@helpdeskpro.local", DepartmentId = 3, Department = itDept, SupervisorId = supervisorId },
                    new() { Id = supervisorId, FullName = "Hendra Supervisor", Email = "sup@helpdeskpro.local", DepartmentId = 3, Department = itDept },
                    new() { Id = _otherUserId, FullName = "Other User", Email = "other@helpdeskpro.local", DepartmentId = 2 }
                }.AsTestDbSet();
                contextMock.Users.Returns(users);

                var ticketsBag = new ConcurrentBag<Ticket>(seedTickets ?? Enumerable.Empty<Ticket>());
                var ticketsSet = (seedTickets ?? new List<Ticket>()).AsTestDbSet();
                ticketsSet.When(s => s.Add(Arg.Any<Ticket>())).Do(ci =>
                {
                    var ticket = ci.Arg<Ticket>();
                    ticket.Id = ticketsBag.Count + 1;
                    ticketsBag.Add(ticket);
                });
                contextMock.Tickets.Returns(ticketsSet);

                var comments = (seedTickets ?? Enumerable.Empty<Ticket>()).SelectMany(t => t.Comments).ToList();
                var commentsSet = comments.AsTestDbSet();
                contextMock.TicketComments.Returns(commentsSet);

                var attachments = (seedTickets ?? Enumerable.Empty<Ticket>()).SelectMany(t => t.Attachments).ToList();
                var attachmentsSet = attachments.AsTestDbSet();
                contextMock.TicketAttachments.Returns(attachmentsSet);

                var histories = (seedTickets ?? Enumerable.Empty<Ticket>()).SelectMany(t => t.Histories).ToList();
                var historiesSet = histories.AsTestDbSet();
                contextMock.TicketHistories.Returns(historiesSet);

                var escalations = (seedTickets ?? Enumerable.Empty<Ticket>()).SelectMany(t => t.Escalations).ToList();
                if (seedEscalations != null) escalations.AddRange(seedEscalations);
                var escalationsSet = escalations.AsTestDbSet();
                contextMock.Escalations.Returns(escalationsSet);

                var notifications = seedNotifications ?? new List<Notification>();
                var notificationsSet = notifications.AsTestDbSet();
                contextMock.Notifications.Returns(notificationsSet);

                var holidaysSet = new List<Holiday>().AsTestDbSet();
                contextMock.Holidays.Returns(holidaysSet);

                contextMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

                services.AddScoped<IApplicationDbContext>(_ => contextMock);
                services.AddSingleton(customGenerator);

                if (customFileStorage != null)
                {
                    services.AddScoped<IFileStorage>(_ => customFileStorage);
                }
            });
        });

        var client = clientFactory.CreateClient();
        var token = GenerateTestJwt(effectiveUserId, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task CreateTicket_WithoutAuthorizationHeader_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var command = new CreateTicketCommand("Tiket Tanpa Auth", "Deskripsi", 1, TicketPriority.Medium);

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTicket_HighPriority_ShouldReturn201WithAccurateSlaDueDate_AC01()
    {
        // Arrange
        var client = CreateCustomClient();
        var beforeTime = DateTime.UtcNow;

        var command = new CreateTicketCommand(
            "Laptop Rusak Parah",
            "Mati total tidak bisa menyala sama sekali",
            1,
            TicketPriority.High);

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        var ticket = content.Data!;
        ticket.TicketNo.Should().StartWith("HD-2026-");
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.Priority.Should().Be(TicketPriority.High);

        var calendar = new BusinessCalendar(Substitute.For<IApplicationDbContext>());
        var calc = new BusinessHoursSlaCalculator(calendar);
        var (expectedResponse, expectedResolution) = calc.CalculateDueDates(beforeTime, 120, 480);

        ticket.ResponseDueAt.Should().BeOnOrAfter(expectedResponse.AddMinutes(-5))
                                    .And.BeOnOrBefore(expectedResponse.AddMinutes(5));
        ticket.ResolutionDueAt.Should().BeOnOrAfter(expectedResolution.AddMinutes(-5))
                                      .And.BeOnOrBefore(expectedResolution.AddMinutes(5));
    }

    [Fact]
    public async Task CreateTicket_20ParallelRequests_ShouldProduce20UniqueAndSequentialTicketNumbers_AC02()
    {
        // Arrange
        var atomicGenerator = new TestAtomicTicketNumberGenerator(2026);
        var client = CreateCustomClient(atomicGenerator);

        const int totalRequests = 20;
        var commands = Enumerable.Range(1, totalRequests)
            .Select(i => new CreateTicketCommand(
                $"Tiket Gangguan Paralel #{i:D2}",
                $"Deskripsi pengujian beban serentak #{i}",
                1,
                TicketPriority.Medium))
            .ToList();

        // Act - Send all 20 requests concurrently in parallel
        var tasks = commands.Select(cmd => client.PostAsJsonAsync("/api/tickets", cmd)).ToList();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(totalRequests);
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
        }

        var results = new List<TicketDetailDto>();
        foreach (var response in responses)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            results.Add(body.Data!);
        }

        var ticketNumbers = results.Select(r => r.TicketNo).ToList();

        // 1. All 20 ticket numbers must be completely unique (no collision)
        ticketNumbers.Distinct().Should().HaveCount(totalRequests);

        // 2. All 20 ticket numbers must match the sequence from 000001 to 000020
        var expectedTicketNumbers = Enumerable.Range(1, totalRequests)
            .Select(i => $"HD-2026-{i:D6}")
            .ToList();

        ticketNumbers.OrderBy(x => x).Should().Equal(expectedTicketNumbers);
    }

    [Fact]
    public async Task GetTickets_WithRoleRequester_ReturnsOnlyOwnTickets()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _testUserId, FullName = "Budi Requester" };
        var otherUser = new ApplicationUser { Id = _otherUserId, FullName = "Other User" };

        var seedTickets = new List<Ticket>
        {
            new()
            {
                Id = 1,
                TicketNo = "HD-2026-000001",
                Title = "Tiket Milik Sendiri",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                RequesterId = _testUserId,
                Requester = requester,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                TicketNo = "HD-2026-000002",
                Title = "Tiket Milik Orang Lain",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.High,
                Status = TicketStatus.Open,
                RequesterId = _otherUserId,
                Requester = otherUser,
                CreatedAt = DateTime.UtcNow
            }
        };

        var client = CreateCustomClient(seedTickets: seedTickets, userId: _testUserId, role: "Requester");

        // Act
        var response = await client.GetAsync("/api/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TicketListItemDto>>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.TotalCount.Should().Be(1);
        content.Data.Items.Should().ContainSingle(t => t.Id == 1 && t.RequesterId == _testUserId);
    }

    [Fact]
    public async Task GetTicketById_RequesterAccessingOthersTicket_Returns404NotFound()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware" };
        var otherUser = new ApplicationUser { Id = _otherUserId, FullName = "Other User" };

        var seedTickets = new List<Ticket>
        {
            new()
            {
                Id = 5,
                TicketNo = "HD-2026-000005",
                Title = "Tiket Orang Lain",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                RequesterId = _otherUserId,
                Requester = otherUser,
                CreatedAt = DateTime.UtcNow
            }
        };

        var client = CreateCustomClient(seedTickets: seedTickets, userId: _testUserId, role: "Requester");

        // Act - Requester tries to access ticket 5 (belongs to _otherUserId)
        var response = await client.GetAsync("/api/tickets/5");

        // Assert - Must return 404 Not Found to prevent existence leakage
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTicketById_RequesterAccessingOwnTicket_HidesInternalComments_AC05()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _testUserId, FullName = "Budi Requester" };
        var agent = new ApplicationUser { Id = _agentUserId, FullName = "Doni Agent" };

        var ticket = new Ticket
        {
            Id = 8,
            TicketNo = "HD-2026-000008",
            Title = "Laptop Bermasalah",
            CategoryId = 1,
            Category = category,
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            RequesterId = _testUserId,
            Requester = requester,
            AssigneeId = _agentUserId,
            Assignee = agent,
            CreatedAt = DateTime.UtcNow
        };

        ticket.Comments.Add(new TicketComment
        {
            Id = 1,
            TicketId = 8,
            AuthorId = _testUserId,
            Author = requester,
            Body = "Mohon segera diperbaiki, laptop kantor",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        });

        ticket.Comments.Add(new TicketComment
        {
            Id = 2,
            TicketId = 8,
            AuthorId = _agentUserId,
            Author = agent,
            Body = "Catatan internal teknisi: RAM rusak perlu ganti",
            IsInternal = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        });

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _testUserId, role: "Requester");

        // Act
        var response = await client.GetAsync("/api/tickets/8");

        // Assert - AC-05: Requester does NOT see internal comment
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        var timeline = content.Data!.Timeline;
        timeline.Should().Contain(t => t.CommentBody == "Mohon segera diperbaiki, laptop kantor");
        timeline.Should().NotContain(t => t.CommentBody == "Catatan internal teknisi: RAM rusak perlu ganti");
    }

    [Fact]
    public async Task GetTicketById_AgentAccessingTicket_ShowsInternalComments_AC05()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _testUserId, FullName = "Budi Requester" };
        var agent = new ApplicationUser { Id = _agentUserId, FullName = "Doni Agent" };

        var ticket = new Ticket
        {
            Id = 9,
            TicketNo = "HD-2026-000009",
            Title = "Laptop Bermasalah Agent View",
            CategoryId = 1,
            Category = category,
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            RequesterId = _testUserId,
            Requester = requester,
            AssigneeId = _agentUserId,
            Assignee = agent,
            CreatedAt = DateTime.UtcNow
        };

        ticket.Comments.Add(new TicketComment
        {
            Id = 1,
            TicketId = 9,
            AuthorId = _testUserId,
            Author = requester,
            Body = "Mohon segera diperbaiki",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        });

        ticket.Comments.Add(new TicketComment
        {
            Id = 2,
            TicketId = 9,
            AuthorId = _agentUserId,
            Author = agent,
            Body = "Catatan internal: stok sparepart tersedia",
            IsInternal = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        });

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        // Act
        var response = await client.GetAsync("/api/tickets/9");

        // Assert - AC-05: Agent DOES see internal comment
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        var timeline = content.Data!.Timeline;
        timeline.Should().Contain(t => t.CommentBody == "Mohon segera diperbaiki");
        timeline.Should().Contain(t => t.CommentBody == "Catatan internal: stok sparepart tersedia" && t.IsInternal);
    }

    [Fact]
    public async Task Assign_ValidRequest_ReturnsOkWithUpdatedTicketAndInProgressStatus()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000010",
            "Tiket Assign IT",
            "Deskripsi tiket",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 10;
        ticket.RowVersion = [1, 2, 3, 4];

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        var payload = new
        {
            AssigneeId = _agentUserId,
            RowVersion = ticket.RowVersion
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/assign", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Status.Should().Be(TicketStatus.InProgress);
        content.Data.AssigneeId.Should().Be(_agentUserId);
        content.Data.FirstRespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_IllegalTransitionDirectlyFromOpenToResolved_Returns422UnprocessableEntity_AC03()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000011",
            "Tiket Transisi Ilegal",
            "Deskripsi tiket",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 11;
        ticket.RowVersion = [1, 2, 3, 4];

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        var payload = new
        {
            Status = TicketStatus.Resolved,
            Reason = "Langsung selesai dari Open",
            RowVersion = ticket.RowVersion
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status", payload);

        // Assert (AC-03)
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().Contain("Perubahan status dari Open ke Resolved tidak diizinkan.");
    }

    [Fact]
    public async Task ChangeStatus_StaleRowVersion_Returns409Conflict()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000012",
            "Tiket Konflik Konkurensi",
            "Deskripsi tiket",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 12;
        ticket.RowVersion = [1, 2, 3, 4];

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        var payload = new
        {
            Status = TicketStatus.InProgress,
            RowVersion = new byte[] { 9, 9, 9, 9 } // Stale RowVersion
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().Contain("Tiket sudah diubah pengguna lain, muat ulang halaman.");
    }

    [Fact]
    public async Task AddComment_FirstCommentByAgent_SetsFirstRespondedAt()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000020",
            "Tiket Komentar Agent",
            "Deskripsi tiket",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 20;

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        var payload = new { Body = "Komentar respons awal dari tim teknis", IsInternal = false };

        // Act
        var response = await client.PostAsJsonAsync($"/api/tickets/{ticket.Id}/comments", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketCommentDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Body.Should().Be("Komentar respons awal dari tim teknis");
        ticket.FirstRespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadAttachment_ExeFile_Returns400BadRequestWithAC06Message()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000021",
            "Tiket Upload Exe",
            "Deskripsi",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 21;

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _testUserId, role: "Requester");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("MZ executable bytes"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        form.Add(fileContent, "file", "malware.exe");

        // Act
        var response = await client.PostAsync($"/api/tickets/{ticket.Id}/attachments", form);

        // Assert (AC-06)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().Contain("Tipe file tidak diizinkan.");
    }

    [Fact]
    public async Task UploadAttachment_PngFile_Returns201Created()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000022",
            "Tiket Upload PNG",
            "Deskripsi",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 22;

        var fileStorageMock = Substitute.For<IFileStorage>();
        fileStorageMock.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("guid-screenshot.png");

        var client = CreateCustomClient(
            seedTickets: new List<Ticket> { ticket },
            userId: _testUserId,
            role: "Requester",
            customFileStorage: fileStorageMock);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]); // PNG header
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "screenshot.png");

        // Act
        var response = await client.PostAsync($"/api/tickets/{ticket.Id}/attachments", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketAttachmentDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.FileName.Should().Be("screenshot.png");
    }

    [Fact]
    public async Task DownloadAttachment_ValidAttachment_ReturnsFileStream()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000023",
            "Tiket Download Lampiran",
            "Deskripsi",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 23;

        var attachment = new TicketAttachment
        {
            Id = 50,
            TicketId = ticket.Id,
            FileName = "laporan.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1234,
            StoragePath = "guid-laporan.pdf",
            UploadedById = _testUserId,
            UploadedAt = DateTime.UtcNow
        };
        ticket.Attachments.Add(attachment);

        var fileStorageMock = Substitute.For<IFileStorage>();
        fileStorageMock.GetFileAsync("guid-laporan.pdf", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("PDF content stream")));

        var client = CreateCustomClient(
            seedTickets: new List<Ticket> { ticket },
            userId: _testUserId,
            role: "Requester",
            customFileStorage: fileStorageMock);

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticket.Id}/attachments/{attachment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var downloadedBytes = await response.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_RequesterUpdatesTicketWhenInProgress_Returns422UnprocessableEntity()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000024",
            "Tiket Sedang InProgress",
            "Deskripsi",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 24;
        ticket.Status = TicketStatus.InProgress;
        ticket.RowVersion = [1, 2, 3, 4];

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _testUserId, role: "Requester");

        var payload = new
        {
            Title = "Judul Baru Requester",
            Description = "Deskripsi baru",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            RowVersion = ticket.RowVersion
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticket.Id}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().Contain("Requester hanya dapat mengubah tiket saat status masih Open.");
    }

    [Fact]
    public async Task Update_AdminUpdatesTicket_Returns200Ok()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000025",
            "Tiket Update Admin",
            "Deskripsi awal",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 25;
        ticket.RowVersion = [1, 2, 3, 4];

        var adminId = Guid.NewGuid();
        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: adminId, role: "Admin");

        var payload = new
        {
            Title = "Judul Diperbarui Admin",
            Description = "Deskripsi diperbarui admin",
            CategoryId = 2,
            Priority = TicketPriority.Critical,
            RowVersion = ticket.RowVersion
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/tickets/{ticket.Id}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Title.Should().Be("Judul Diperbarui Admin");
        content.Data.CategoryId.Should().Be(2);
        content.Data.Priority.Should().Be(TicketPriority.Critical);
    }

    [Fact]
    public async Task GetHistory_ReturnsChronologicalList()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000026",
            "Tiket Riwayat",
            "Deskripsi",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 26;

        ticket.Histories.Add(new TicketHistory
        {
            Id = 1,
            TicketId = ticket.Id,
            FieldName = "Title",
            OldValue = "Judul Lama",
            NewValue = "Tiket Riwayat",
            ChangedById = _testUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _testUserId, role: "Requester");

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticket.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TicketHistoryDto>>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeEmpty();
        content.Data.Should().Contain(h => h.FieldName == "Title" && h.NewValue == "Tiket Riwayat");
    }

    [Fact]
    public async Task Escalate_AgentManualEscalation_Returns200Ok()
    {
        // Arrange
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var ticket = Ticket.Create(
            "HD-2026-000030",
            "Tiket Butuh Eskalasi",
            "Deskripsi tiket",
            1,
            TicketPriority.Medium,
            _testUserId,
            slaPolicy,
            DateTime.UtcNow,
            "budi@helpdeskpro.local");
        ticket.Id = 30;

        var client = CreateCustomClient(seedTickets: new List<Ticket> { ticket }, userId: _agentUserId, role: "Agent");

        var payload = new { Note = "Eskalasi manual dari agent lapangan karena isu server kompleks" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/tickets/{ticket.Id}/escalate", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<HelpDeskPro.Application.Features.Escalations.Models.EscalationDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Reason.Should().Be(EscalationReason.Manual);
        content.Data.Note.Should().Be("Eskalasi manual dari agent lapangan karena isu server kompleks");
    }

    [Fact]
    public async Task Acknowledge_RecipientAcknowledgesEscalation_Returns200Ok()
    {
        // Arrange
        var supervisorId = Guid.Parse("44444444-5555-6666-7777-888888888888");
        var escalation = new Escalation
        {
            Id = 100,
            TicketId = 1,
            Level = 1,
            Reason = EscalationReason.SlaResponseBreach,
            EscalatedToId = supervisorId,
            EscalatedTo = new ApplicationUser { Id = supervisorId, FullName = "Hendra Supervisor" },
            EscalatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var client = CreateCustomClient(
            userId: supervisorId,
            role: "Supervisor",
            seedEscalations: new List<Escalation> { escalation });

        // Act
        var response = await client.PatchAsync($"/api/escalations/{escalation.Id}/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<HelpDeskPro.Application.Features.Escalations.Models.EscalationDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.AcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Notifications_GetList_ReturnsUserNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = _testUserId, Message = "Notifikasi Budi", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = Guid.NewGuid(), Message = "Notifikasi Orang Lain", IsRead = false, CreatedAt = DateTime.UtcNow }
        };

        var client = CreateCustomClient(userId: _testUserId, role: "Requester", seedNotifications: notifications);

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<HelpDeskPro.Application.Features.Notifications.Models.NotificationDto>>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().ContainSingle(n => n.Message == "Notifikasi Budi");
    }

    [Fact]
    public async Task Notifications_GetUnreadCount_ReturnsCount()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = _testUserId, Message = "Notif Unread 1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = _testUserId, Message = "Notif Unread 2", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, UserId = _testUserId, Message = "Notif Read", IsRead = true, CreatedAt = DateTime.UtcNow }
        };

        var client = CreateCustomClient(userId: _testUserId, role: "Requester", seedNotifications: notifications);

        // Act
        var response = await client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().Be(2);
    }

    [Fact]
    public async Task Notifications_MarkAllAsRead_ReturnsCountAndUpdatesNotifications()
    {
        // Arrange
        var notif1 = new Notification { Id = 1, UserId = _testUserId, Message = "Notif 1", IsRead = false, CreatedAt = DateTime.UtcNow };
        var notif2 = new Notification { Id = 2, UserId = _testUserId, Message = "Notif 2", IsRead = false, CreatedAt = DateTime.UtcNow };

        var client = CreateCustomClient(userId: _testUserId, role: "Requester", seedNotifications: new List<Notification> { notif1, notif2 });

        // Act
        var response = await client.PatchAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().Be(2);
        notif1.IsRead.Should().BeTrue();
        notif2.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task RateTicket_WhenResolvedAndByRequester_ReturnsOkAndTicketIsClosed()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 888,
            TicketNo = "HD-2026-000888",
            Title = "Tiket untuk dirating",
            Status = TicketStatus.Resolved,
            RequesterId = _testUserId,
            ResolvedAt = DateTime.UtcNow.AddHours(-1)
        };

        var client = CreateCustomClient(userId: _testUserId, role: "Requester", seedTickets: new List<Ticket> { ticket });

        var payload = new { Rating = 5, Comment = "Pelayanan luar biasa mantap!" };

        // Act
        var response = await client.PostAsJsonAsync("/api/tickets/888/rate", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Rating.Should().Be(5);
        content.Data.RatingComment.Should().Be("Pelayanan luar biasa mantap!");
        content.Data.Status.Should().Be(TicketStatus.Closed);
    }

    private sealed class TestAtomicTicketNumberGenerator : ITicketNumberGenerator
    {
        private readonly int _year;
        private int _counter;

        public TestAtomicTicketNumberGenerator(int year)
        {
            _year = year;
        }

        public Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default)
        {
            var next = Interlocked.Increment(ref _counter);
            return Task.FromResult(TicketNumberGenerator.Format(_year, next));
        }
    }
}
