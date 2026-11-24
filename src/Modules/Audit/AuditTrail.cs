using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Auditing;

namespace VCare.Modules.Audit;

/// <summary>One entry of a resident's audit trail. Changes is the JSON the change was recorded with.</summary>
public sealed record AuditEntryResponse(DateTime OccurredAt, Guid? StaffId, string Action, string EntityType,
    string EntityId, string? Detail, JsonElement? Changes);

internal sealed class AuditTrail(AuditDbContext context, ICurrentUser currentUser, TimeProvider clock)
{
    internal const int MaxPage = 500;

    /// <summary>A member of staff opened part of a resident's record.</summary>
    public async Task RecordViewAsync(Guid residentId, string detail, CancellationToken cancellationToken)
    {
        context.Entries.Add(AuditEntry.Create(currentUser.CareHomeId, clock.GetUtcNow().UtcDateTime, currentUser.UserId,
            AuditAction.Viewed, "Resident", residentId.ToString(), residentId, null, detail,
            Activity.Current?.TraceId.ToString()));
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Newest first; pass the oldest OccurredAt seen as <paramref name="before"/> for
    /// the next page. Every entry of one save shares its moment, so a page never ends
    /// partway through one: it takes the rest of that moment too, and can run a few
    /// entries over <paramref name="take"/>.
    /// </summary>
    public async Task<IReadOnlyList<AuditEntryResponse>> ForResidentAsync(Guid residentId, DateTime? before, int take,
        CancellationToken cancellationToken)
    {
        var page = Math.Clamp(take, 1, MaxPage);
        var trail = context.Entries.AsNoTracking().Where(e => e.ResidentId == residentId);
        var entries = await trail
            .Where(e => before == null || e.OccurredAt < before)
            .OrderByDescending(e => e.OccurredAt)
            .Take(page)
            .ToListAsync(cancellationToken);
        if (entries.Count == page)
        {
            var last = entries[^1].OccurredAt;
            var onPage = entries.Where(e => e.OccurredAt == last).Select(e => e.Id).ToList();
            entries.AddRange(await trail
                .Where(e => e.OccurredAt == last && !onPage.Contains(e.Id))
                .ToListAsync(cancellationToken));
        }
        return [.. entries.Select(e => new AuditEntryResponse(e.OccurredAt, e.StaffId, e.Action, e.EntityType, e.EntityId,
            e.Detail, e.Changes is null ? null : JsonDocument.Parse(e.Changes).RootElement.Clone()))];
    }
}
