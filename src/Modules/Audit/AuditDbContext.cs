using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Auditing;

namespace VCare.Modules.Audit;

/// <summary>
/// Owns the audit table. Every other module writes to it through its own
/// context, in the same save as the change; this one creates it, reads it, and
/// records views, which change nothing else.
/// </summary>
internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    // Read per query, so a caller only ever sees their own home's trail.
    private Guid? CurrentCareHome => currentUser.CareHomeId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AuditEntry.Schema);
        modelBuilder.MapAuditEntries(ownsTable: true);
        modelBuilder.Entity<AuditEntry>().HasQueryFilter(e => e.CareHomeId == CurrentCareHome);
    }
}
