using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Infrastructure.Persistence.Configurations;

internal sealed class CarePlanConfiguration : IEntityTypeConfiguration<CarePlan>
{
    public void Configure(EntityTypeBuilder<CarePlan> builder)
    {
        builder.ToTable("CarePlans", CarePlanDbContext.Schema);
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.CareHomeId)
            .HasConversion(id => id.Value, value => new CareHomeId(value))
            .IsRequired();
        builder.HasIndex(cp => cp.CareHomeId);
        builder.HasIndex(cp => cp.PatientId).IsUnique();


        // Diagnoses, goals and interventions are owned by the care plan; they
        // have no identity or lifecycle of their own.
        builder.OwnsMany(cp => cp.Diagnoses, d => d.ToTable("CarePlanDiagnoses", CarePlanDbContext.Schema));

        // Two people saving edits to the same version: the second save finds the
        // version moved on and fails, instead of overwriting the first.
        builder.Property(cp => cp.Version).IsConcurrencyToken();
        builder.HasMany(cp => cp.Versions).WithOne().HasForeignKey(v => v.CarePlanId).OnDelete(DeleteBehavior.Cascade);
        // Domain events are behaviour, not persisted state.
        builder.Ignore(cp => cp.DomainEvents);
    }
}

internal sealed class CarePlanVersionConfiguration : IEntityTypeConfiguration<CarePlanVersion>
{
    public void Configure(EntityTypeBuilder<CarePlanVersion> builder)
    {
        builder.ToTable("CarePlanVersions", CarePlanDbContext.Schema);
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Goal).IsRequired();
        builder.Property(v => v.Diagnoses).IsRequired();
        builder.HasIndex(v => new { v.CarePlanId, v.Number }).IsUnique();
    }
}
