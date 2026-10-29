using System.Text.Json;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenOps.Infrastructure.Persistence;

/// <summary>
/// Her SaveChanges'ta eklenen/değişen/silinen kayıtları aynı transaction içinde AuditLogs'a yazar.
/// Gizli alanlar (şifre hash'i, token, 2FA secret, API anahtarı) loglanmaz.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> IgnoredEntities = new()
    {
        typeof(AuditLog), typeof(RefreshToken), typeof(Notification), typeof(IotMeterReading)
    };

    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AppUser.PasswordHash), nameof(AppUser.TwoFactorSecret), nameof(AppUser.PasswordResetToken),
        nameof(RefreshToken.Token), nameof(ApiKey.Key), nameof(License.LicenseKey)
    };

    private readonly ICurrentUserAccessor _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUserAccessor currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void AddAuditEntries(DbContext context)
    {
        // Seed/migration gibi kullanıcısız işlemler loglanmaz.
        if (_currentUser.UserId is null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                        && !IgnoredEntities.Contains(e.Entity.GetType()))
            .ToList();

        foreach (var entry in entries)
        {
            var (oldValues, newValues) = CollectValues(entry);
            if (entry.State == EntityState.Modified && newValues.Count == 0)
                continue;

            context.Set<AuditLog>().Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId,
                UserName = _currentUser.UserName ?? string.Empty,
                CompanyId = ReadGuid(entry, "CompanyId") ?? _currentUser.CompanyId,
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Deleted => "Delete",
                    _ => "Update"
                },
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = ReadGuid(entry, "Id")?.ToString(),
                OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private static (Dictionary<string, object?> Old, Dictionary<string, object?> New) CollectValues(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            var sensitive = SensitiveProperties.Contains(name);

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = sensitive ? "***" : property.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValues[name] = sensitive ? "***" : property.OriginalValue;
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    oldValues[name] = sensitive ? "***" : property.OriginalValue;
                    newValues[name] = sensitive ? "***" : property.CurrentValue;
                    break;
            }
        }

        return (oldValues, newValues);
    }

    private static Guid? ReadGuid(EntityEntry entry, string propertyName)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        if (property is null) return null;
        return entry.Property(propertyName).CurrentValue switch
        {
            Guid g when g != Guid.Empty => g,
            _ => null
        };
    }
}
