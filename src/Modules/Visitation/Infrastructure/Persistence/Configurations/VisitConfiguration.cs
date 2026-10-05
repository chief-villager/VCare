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

        // Conversion comes from ConfigureConventions, like the other typed ids:
        // type-wide, so a new entity carrying a CareHomeId is covered without
        // anyone remembering to add a line here.
        builder.Property(v => v.CareHomeId).IsRequired();
        builder.HasIndex(v => v.CareHomeId);
        builder.HasIndex(v => v.PatientId);

        // Stored as the name, so a reordered enum cannot silently reinterpret
        // rows that are already in the table.
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CancellationReason).HasMaxLength(500);

        // The three task records have no key and no life of their own -- a feeding
        // task is meaningless without its visit -- so they are owned and stored on
        // the Visits row by table splitting, rather than as three extra tables
        // keyed by VisitId each holding one bool and one note.
        builder.OwnsOne(v => v.FeedingTask);
        builder.OwnsOne(v => v.MedicationTask);
        builder.OwnsOne(v => v.PersonalcareTask);

        // Domain events are behaviour, not persisted state.
        builder.Ignore(v => v.DomainEvents);
    }
}
