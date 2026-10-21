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
        // Domain events are behaviour, not persisted state.
        builder.Ignore(cp => cp.DomainEvents);
    }
}
