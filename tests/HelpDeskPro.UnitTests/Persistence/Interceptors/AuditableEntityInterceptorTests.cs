namespace HelpDeskPro.UnitTests.Persistence.Interceptors;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Common;
using HelpDeskPro.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

public class AuditableEntityInterceptorTests
{
    private sealed class TestAuditableEntity : AuditableEntity<long>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestAuditableEntity> TestEntities => Set<TestAuditableEntity>();
    }

    [Fact]
    public void SavingChanges_WhenEntityAdded_ShouldPopulateCreatedAtAndCreatedBy()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        const string userId = "user-123";

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(userId);

        var interceptor = new AuditableEntityInterceptor(dateTimeProvider, currentUserService);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Name = "Test" };
        context.TestEntities.Add(entity);

        // Act
        interceptor.UpdateAuditEntities(context);

        // Assert
        entity.CreatedAt.Should().Be(now);
        entity.CreatedBy.Should().Be(userId);
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void SavingChanges_WhenEntityAddedWithExplicitCreatedAt_ShouldKeepIt()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        var historical = now.AddDays(-5);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);
        var currentUserService = Substitute.For<ICurrentUserService>();

        var interceptor = new AuditableEntityInterceptor(dateTimeProvider, currentUserService);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Name = "Seed", CreatedAt = historical };
        context.TestEntities.Add(entity);

        // Act
        interceptor.UpdateAuditEntities(context);

        // Assert
        entity.CreatedAt.Should().Be(historical);
        entity.CreatedBy.Should().Be("system");
    }

    [Fact]
    public void SavingChanges_WhenEntityDeleted_ShouldConvertToSoftDelete()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        const string userId = "user-123";

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(userId);

        var interceptor = new AuditableEntityInterceptor(dateTimeProvider, currentUserService);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Id = 1, Name = "Test" };
        context.TestEntities.Attach(entity);
        context.TestEntities.Remove(entity);

        // Act
        interceptor.UpdateAuditEntities(context);

        // Assert
        context.Entry(entity).State.Should().Be(EntityState.Modified);
        entity.IsDeleted.Should().BeTrue();
        entity.UpdatedAt.Should().Be(now);
        entity.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public void SavingChanges_WhenNoCurrentUser_ShouldUseSystemAndNeverWriteGuidEmpty()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns((string?)null);

        var interceptor = new AuditableEntityInterceptor(dateTimeProvider, currentUserService);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Name = "SystemAction" };
        context.TestEntities.Add(entity);

        // Act
        interceptor.UpdateAuditEntities(context);

        // Assert
        entity.CreatedBy.Should().Be("system");
        entity.CreatedBy.Should().NotBe(Guid.Empty.ToString());
    }

    [Fact]
    public void SavingChanges_WhenUserIdIsGuidEmpty_ShouldUseSystem()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.Empty.ToString());

        var interceptor = new AuditableEntityInterceptor(dateTimeProvider, currentUserService);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestAuditableEntity { Name = "EmptyGuidAction" };
        context.TestEntities.Add(entity);

        // Act
        interceptor.UpdateAuditEntities(context);

        // Assert
        entity.CreatedBy.Should().Be("system");
        entity.CreatedBy.Should().NotBe(Guid.Empty.ToString());
    }
}
