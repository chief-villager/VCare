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

        // Stored as the name, so a reordered enum cannot silently reinterpret
        // rows that are already in the table.
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CancellationReason).HasMaxLength(500);

        // Domain events are behaviour, not persisted state.
        builder.Ignore(v => v.DomainEvents);
    }
}
