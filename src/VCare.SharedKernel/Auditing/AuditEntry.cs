namespace VCare.SharedKernel.Auditing;

/// <summary>
/// One thing someone did to, or saw of, a record: who, when, what, and for a
/// change, each value before and after. Never updated or deleted.
/// </summary>
public sealed class AuditEntry
{
    public const string Schema = "audit";
    public const string Table = "AuditEntries";

    public Guid Id { get; private set; }

    /// <summary>The care home the record belongs to.</summary>
    public Guid? CareHomeId { get; private set; }

    /// <summary>UTC.</summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>Who did it; null for the system, such as the missed-dose job.</summary>
    public Guid? StaffId { get; private set; }

    /// <summary>One of <see cref="AuditAction"/>.</summary>
    public string Action { get; private set; } = null!;

    /// <summary>The kind of record, such as PatientAllergy or CarePlan.</summary>
    public string EntityType { get; private set; } = null!;

    public string EntityId { get; private set; } = null!;

    /// <summary>The resident the record is about, when it is about one.</summary>
    public Guid? ResidentId { get; private set; }

    /// <summary>
    /// JSON. For a record created or deleted, its values; for a change, each value
    /// that changed as { "from": ..., "to": ... }. Null for a view.
    /// </summary>
    public string? Changes { get; private set; }

    /// <summary>For a view, the route that was read.</summary>
    public string? Detail { get; private set; }

    /// <summary>The trace the request ran in, to find its spans.</summary>
    public string? TraceId { get; private set; }

    private AuditEntry() { }

    public static AuditEntry Create(Guid? careHomeId, DateTime occurredAt, Guid? staffId, string action, string entityType,
        string entityId, Guid? residentId, string? changes, string? detail, string? traceId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CareHomeId = careHomeId,
            OccurredAt = occurredAt,
            StaffId = staffId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ResidentId = residentId,
            Changes = changes,
            Detail = detail,
            TraceId = traceId
        };
}

public static class AuditAction
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Viewed = "Viewed";
}

/// <summary>
/// Marks an entity whose changes go in the audit trail. Opt-in, so that tables
/// holding secrets (password hashes, token hashes) are never copied into it.
/// </summary>
public interface IAudited;
