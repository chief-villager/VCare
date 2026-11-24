using Microsoft.EntityFrameworkCore;

namespace VCare.SharedKernel.Auditing;

public static class AuditModel
{
    /// <summary>
    /// Maps the shared audit table into a module's model, so the module writes its
    /// audit rows in the same transaction as the change they describe. Only the
    /// Audit module owns the table; every other module leaves it out of its migrations.
    /// </summary>
    public static ModelBuilder MapAuditEntries(this ModelBuilder modelBuilder, bool ownsTable = false)
    {
        modelBuilder.Entity<AuditEntry>(entry =>
        {
            entry.ToTable(AuditEntry.Table, AuditEntry.Schema, table =>
            {
                if (!ownsTable)
                {
                    table.ExcludeFromMigrations();
                }
            });
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).ValueGeneratedNever();
            entry.Property(e => e.Action).HasMaxLength(20).IsUnicode(false);
            entry.Property(e => e.EntityType).HasMaxLength(100);
            entry.Property(e => e.EntityId).HasMaxLength(100);
            entry.Property(e => e.Detail).HasMaxLength(300);
            entry.Property(e => e.TraceId).HasMaxLength(32).IsUnicode(false);
            // A resident's history, newest first; and one record's history.
            entry.HasIndex(e => new { e.ResidentId, e.OccurredAt });
            entry.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAt });
        });
        return modelBuilder;
    }
}
