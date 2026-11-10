namespace HelpDeskPro.UnitTests.Features.Notifications;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Notifications.Commands.MarkNotificationsAsRead;
using HelpDeskPro.Application.Features.Notifications.Queries.GetNotifications;
using HelpDeskPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class NotificationTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public NotificationTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _currentUserService.UserId.Returns(_userId.ToString());
    }

    [Fact]
    public async Task GetNotifications_ReturnsUserNotificationsOrderedByCreatedAtDescending()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = _userId, Message = "Notifikasi lama", IsRead = true, CreatedAt = _utcNow.AddHours(-2) },
            new() { Id = 2, UserId = _userId, Message = "Notifikasi baru", IsRead = false, CreatedAt = _utcNow.AddHours(-1) },
            new() { Id = 3, UserId = Guid.NewGuid(), Message = "Notifikasi orang lain", IsRead = false, CreatedAt = _utcNow }
        };

        var notificationsSet = notifications.AsTestDbSet();
        _context.Notifications.Returns(notificationsSet);

        var handler = new GetNotificationsQueryHandler(_context, _currentUserService);

        // Act
        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(2); // Baru
        result[1].Id.Should().Be(1); // Lama
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsCorrectCount()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = _userId, Message = "Notif 1", IsRead = false, CreatedAt = _utcNow },
            new() { Id = 2, UserId = _userId, Message = "Notif 2", IsRead = false, CreatedAt = _utcNow },
            new() { Id = 3, UserId = _userId, Message = "Notif 3", IsRead = true, CreatedAt = _utcNow },
            new() { Id = 4, UserId = Guid.NewGuid(), Message = "Notif orang lain", IsRead = false, CreatedAt = _utcNow }
        };

        var notificationsSet = notifications.AsTestDbSet();
        _context.Notifications.Returns(notificationsSet);

        var handler = new GetUnreadNotificationCountQueryHandler(_context, _currentUserService);

        // Act
        var result = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task MarkNotificationsAsRead_UpdatesAllUnreadToTrue()
    {
        // Arrange
        var notif1 = new Notification { Id = 1, UserId = _userId, Message = "Notif 1", IsRead = false, CreatedAt = _utcNow };
        var notif2 = new Notification { Id = 2, UserId = _userId, Message = "Notif 2", IsRead = false, CreatedAt = _utcNow };
        var notif3 = new Notification { Id = 3, UserId = _userId, Message = "Notif 3", IsRead = true, CreatedAt = _utcNow };

        var notificationsSet = new List<Notification> { notif1, notif2, notif3 }.AsTestDbSet();
        _context.Notifications.Returns(notificationsSet);

        var handler = new MarkNotificationsAsReadCommandHandler(_context, _currentUserService);

        // Act
        var updatedCount = await handler.Handle(new MarkNotificationsAsReadCommand(), CancellationToken.None);

        // Assert
        updatedCount.Should().Be(2);
        notif1.IsRead.Should().BeTrue();
        notif2.IsRead.Should().BeTrue();
        notif3.IsRead.Should().BeTrue();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

