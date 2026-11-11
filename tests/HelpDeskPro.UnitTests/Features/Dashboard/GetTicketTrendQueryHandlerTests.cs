namespace HelpDeskPro.UnitTests.Features.Dashboard;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetTicketTrend;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetTicketTrendQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    // 10 Nov 2026 10:00 UTC = 10 Nov 2026 17:00 WIB
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public GetTicketTrendQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Theory]
    [InlineData("Requester")]
    [InlineData("Agent")]
    public async Task Handle_WhenUserIsNotSupervisorOrAdmin_ThrowsForbiddenAccessException(string role)
    {
        _currentUserService.Role.Returns(role);
        var handler = new GetTicketTrendQueryHandler(_context, _currentUserService, _dateTimeProvider);

        var act = async () => await handler.Handle(new GetTicketTrendQuery(7), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAdmin_ReturnsOnePointPerDayGroupedByWibDate()
    {
        _currentUserService.Role.Returns("Admin");
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());

        var tickets = new List<Ticket>
        {
            // 9 Nov 20:00 UTC = 10 Nov 03:00 WIB -> dihitung pada 10 Nov
            new() { Id = 1, CreatedAt = new DateTime(2026, 11, 9, 20, 0, 0, DateTimeKind.Utc) },
            // 10 Nov 02:00 UTC = 10 Nov 09:00 WIB, selesai 10 Nov 05:00 UTC
            new()
            {
                Id = 2,
                CreatedAt = new DateTime(2026, 11, 10, 2, 0, 0, DateTimeKind.Utc),
                ResolvedAt = new DateTime(2026, 11, 10, 5, 0, 0, DateTimeKind.Utc)
            },
            // 8 Nov 05:00 UTC = 8 Nov 12:00 WIB
            new() { Id = 3, CreatedAt = new DateTime(2026, 11, 8, 5, 0, 0, DateTimeKind.Utc) },
            // Di luar rentang 7 hari: dibuat 1 Nov, tetapi diselesaikan 9 Nov
            new()
            {
                Id = 4,
                CreatedAt = new DateTime(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc),
                ResolvedAt = new DateTime(2026, 11, 9, 5, 0, 0, DateTimeKind.Utc)
            }
        };
        var ticketSet = tickets.AsTestDbSet();
        _context.Tickets.Returns(ticketSet);

        var handler = new GetTicketTrendQueryHandler(_context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetTicketTrendQuery(7), CancellationToken.None);

        result.Should().HaveCount(7);
        result[0].Date.Should().Be(new DateOnly(2026, 11, 4));
        result[^1].Date.Should().Be(new DateOnly(2026, 11, 10));

        var byDate = result.ToDictionary(p => p.Date);
        byDate[new DateOnly(2026, 11, 10)].CreatedCount.Should().Be(2);
        byDate[new DateOnly(2026, 11, 10)].ResolvedCount.Should().Be(1);
        byDate[new DateOnly(2026, 11, 9)].CreatedCount.Should().Be(0);
        byDate[new DateOnly(2026, 11, 9)].ResolvedCount.Should().Be(1);
        byDate[new DateOnly(2026, 11, 8)].CreatedCount.Should().Be(1);
        result.Sum(p => p.CreatedCount).Should().Be(3);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(7, true)]
    [InlineData(31, true)]
    [InlineData(32, false)]
    public void Validator_ValidatesDaysRange(int days, bool expectedValid)
    {
        var validator = new GetTicketTrendQueryValidator();

        var result = validator.Validate(new GetTicketTrendQuery(days));

        result.IsValid.Should().Be(expectedValid);
    }
}
