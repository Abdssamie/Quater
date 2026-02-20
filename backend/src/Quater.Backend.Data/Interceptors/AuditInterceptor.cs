using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Interfaces;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically populates audit fields for entities implementing IAuditable.
/// Sets CreatedAt/CreatedBy on insert and UpdatedAt/UpdatedBy on update.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public AuditInterceptor(ICurrentUserService currentUserService, TimeProvider? timeProvider = null)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditInfo(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditInfo(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInfo(DbContext context)
    {
        var userId = _currentUserService.GetCurrentUserIdOrSystem();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(e => e.Entity is IAuditable)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetProperty(entry.Entity, "CreatedAt", now);
                SetProperty(entry.Entity, "CreatedBy", userId);
            }

            if (entry.State == EntityState.Modified)
            {
                SetProperty(entry.Entity, "UpdatedAt", now);
                SetProperty(entry.Entity, "UpdatedBy", userId);
            }
        }
    }

    private static void SetProperty(object entity, string propertyName, object value)
    {
        var property = entity.GetType().GetProperty(propertyName);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(entity, value);
        }
    }
}
