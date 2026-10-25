using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Infrastructure.Persistence.Configurations;

internal sealed class VisitRecordConfiguration : IEntityTypeConfiguration<VisitRecord>
{
    public void Configure(EntityTypeBuilder<VisitRecord> builder)
    {
        builder.ToTable("VisitRecords", VisitationDbContext.Schema);
        builder.HasKey(v => v.Id);

        // The domain sets the id. Without this EF takes a record added to a loaded
        // visit for an existing row, and check-in fails with an UPDATE of nothing.
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.HasIndex(v => v.StaffId);

        // Stored as the name, so a reordered enum cannot silently reinterpret
        // rows that are already in the table.
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // The three task records have no key and no life of their own -- a feeding
        // task is meaningless without its record -- so they are owned and stored on
        // the VisitRecords row by table splitting, rather than as three extra tables
        // each holding one bool and one note.
        builder.OwnsOne(v => v.FeedingTask);
        builder.OwnsOne(v => v.MedicationTask);
        builder.OwnsOne(v => v.PersonalcareTask);
    }
}

internal sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits", VisitationDbContext.Schema);
        builder.HasKey(v => v.Id);

        // Typed ids are converted by ConfigureConventions, type-wide.
        builder.Property(v => v.CareHomeId).IsRequired();
        builder.HasIndex(v => v.CareHomeId);
        builder.HasIndex(v => v.PatientId);

        builder.Property(v => v.CancellationReason).HasMaxLength(500);

        // Stored as a JSON array of guids on the visit row. The convention only
        // covers scalar properties, so the element conversion is spelled out here.
        builder.PrimitiveCollection(v => v.StaffIds)
            .ElementType(e => e.HasConversion<VisitStaffTypedIdConverter>());

        builder.HasMany(v => v.VisitRecords)
            .WithOne()
            .HasForeignKey(r => r.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are behaviour, not persisted state.
        builder.Ignore(v => v.DomainEvents);
    }
}
