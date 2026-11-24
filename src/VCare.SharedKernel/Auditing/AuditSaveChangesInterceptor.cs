using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using VCare.SharedKernel.Abstractions;

namespace VCare.SharedKernel.Auditing;

/// <summary>
/// Adds an <see cref="AuditEntry"/> for every audited entity a save creates,
/// changes or deletes, to the same save: the change and its record of who made
/// it commit or roll back together.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    private const int MaxDepth = 4;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }
        // EF detects changes after this hook runs; the before-and-after values
        // are needed now.
        context.ChangeTracker.DetectChanges();
        var changed = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAudited && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (changed.Count == 0)
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var traceId = Activity.Current?.TraceId.ToString();
        Guid? staffId = currentUser.UserId == Guid.Empty ? null : currentUser.UserId;
        var callerHome = currentUser.CareHomeId == Guid.Empty ? (Guid?)null : currentUser.CareHomeId;

        var entries = new List<AuditEntry>();
        foreach (var entry in changed)
        {
            var changes = ChangesOf(entry);
            if (changes is null)
            {
                continue; // marked modified, but no value actually changed
            }
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };
            entries.Add(AuditEntry.Create(callerHome ?? CareHomeOf(entry, 0), now, staffId, action,
                entry.Metadata.ClrType.Name, KeyOf(entry), ResidentOf(entry, 0), changes, null, traceId));
        }
        context.AddRange(entries);
    }

    private static string? ChangesOf(EntityEntry entry)
    {
        var values = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }
            var name = property.Metadata.Name;
            switch (entry.State)
            {
                case EntityState.Added:
                    values[name] = Stored(property, property.CurrentValue);
                    break;
                case EntityState.Deleted:
                    values[name] = Stored(property, property.OriginalValue);
                    break;
                case EntityState.Modified when property.IsModified:
                    var from = Stored(property, property.OriginalValue);
                    var to = Stored(property, property.CurrentValue);
                    if (!Equals(from, to))
                    {
                        values[name] = new { from, to };
                    }
                    break;
            }
        }
        return entry.State == EntityState.Modified && values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }

    /// <summary>The value as the database stores it: an id's Guid, an enum's name.</summary>
    private static object? Stored(PropertyEntry property, object? value) =>
        value is null ? null : property.Metadata.GetTypeMapping().Converter?.ConvertToProvider(value) ?? value;

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }
        return string.Join("/", key.Properties.Select(p =>
        {
            var property = entry.Property(p.Name);
            return Convert.ToString(Stored(property, property.CurrentValue ?? property.OriginalValue),
                System.Globalization.CultureInfo.InvariantCulture);
        }));
    }

    /// <summary>
    /// The resident a record is about: the resident themselves (a key that is a
    /// PatientId), a record naming one (a PatientId property), or a record that
    /// belongs to one of those (found through its tracked parent).
    /// </summary>
    private static Guid? ResidentOf(EntityEntry entry, int depth)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key?.Properties is [var only] && only.ClrType == typeof(PatientId))
        {
            return AsGuid(ValueOf(entry, only.Name));
        }
        if (entry.Metadata.FindProperty("PatientId") is not null)
        {
            return AsGuid(ValueOf(entry, "PatientId"));
        }
        return depth >= MaxDepth ? null : ParentsOf(entry).Select(p => ResidentOf(p, depth + 1)).FirstOrDefault(r => r is not null);
    }

    private static Guid? CareHomeOf(EntityEntry entry, int depth)
    {
        if (entry.Metadata.FindProperty("CareHomeId") is not null)
        {
            return AsGuid(Stored(entry.Property("CareHomeId"), ValueOf(entry, "CareHomeId")));
        }
        if (entry.Metadata.ClrType.Name == "CareHome" && entry.Metadata.FindPrimaryKey()?.Properties is [var only])
        {
            return AsGuid(Stored(entry.Property(only.Name), ValueOf(entry, only.Name)));
        }
        return depth >= MaxDepth ? null : ParentsOf(entry).Select(p => CareHomeOf(p, depth + 1)).FirstOrDefault(c => c is not null);
    }

    private static IEnumerable<EntityEntry> ParentsOf(EntityEntry entry)
    {
        foreach (var foreignKey in entry.Metadata.GetForeignKeys())
        {
            var values = foreignKey.Properties.Select(p => ValueOf(entry, p.Name)).ToArray();
            if (values.Any(v => v is null))
            {
                continue;
            }
            var parent = entry.Context.ChangeTracker.Entries().FirstOrDefault(candidate =>
                foreignKey.PrincipalEntityType.IsAssignableFrom(candidate.Metadata)
                && foreignKey.PrincipalKey.Properties.Select(p => ValueOf(candidate, p.Name)).SequenceEqual(values));
            if (parent is not null)
            {
                yield return parent;
            }
        }
    }

    private static object? ValueOf(EntityEntry entry, string property)
    {
        var value = entry.Property(property);
        return value.CurrentValue ?? value.OriginalValue;
    }

    private static Guid? AsGuid(object? value) => value switch
    {
        Guid g when g != Guid.Empty => g,
        PatientId p => p.Value,
        CareHomeId c => c.Value,
        _ => null
    };
}
