using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Visitation.Infrastructure.Persistence.Configurations;

internal sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits", VisitationDbContext.Schema);
        builder.HasKey(v => v.Id);

        builder.Property(v => v.CareHomeId)
            .HasConversion(id => id.Value, value => new CareHomeId(value))
            .IsRequired();
        builder.HasIndex(v => v.CareHomeId);
        builder.HasIndex(v => v.PatientId);

        // The front desk's main query: today's visits for this care home.
        builder.HasIndex(v => new { v.CareHomeId, v.ScheduledFor });

        // Stored as the name, so a reordered enum cannot silently reinterpret
        // rows that are already in the table.
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.Purpose).HasMaxLength(500);
        builder.Property(v => v.CancellationReason).HasMaxLength(500);

        // Visitors are owned by the visit; they have no identity or lifecycle
        // of their own.
        builder.OwnsMany(v => v.Visitors, v =>
        {
            v.ToTable("VisitVisitors", VisitationDbContext.Schema);
            v.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            v.Property(x => x.Relationship).HasMaxLength(100).IsRequired();
            v.Property(x => x.PhoneNumber).HasMaxLength(30);
        });

        // Domain events are behaviour, not persisted state.
        builder.Ignore(v => v.DomainEvents);
    }
}
