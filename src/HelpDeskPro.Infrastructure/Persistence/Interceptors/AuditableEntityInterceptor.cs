namespace HelpDeskPro.Infrastructure.Persistence.Interceptors;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateAuditEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId;
        var auditActor = string.IsNullOrWhiteSpace(userId) || userId == Guid.Empty.ToString()
            ? "system"
            : userId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Pertahankan CreatedAt yang diisi eksplisit (mis. data seeding historis);
                    // hanya isi otomatis jika belum diset.
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = utcNow;
                    }

                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) || entry.Entity.CreatedBy == Guid.Empty.ToString())
                    {
                        entry.Entity.CreatedBy = auditActor;
                    }
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = auditActor;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = auditActor;
                    break;
            }
        }
    }
}

