using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Infrastructure.Persistence.Configurations;

internal sealed class CarePlanConfiguration : IEntityTypeConfiguration<CarePlan>
{
    public void Configure(EntityTypeBuilder<CarePlan> builder)
    {
        builder.ToTable("CarePlans", PatientsDbContext.Schema);
        builder.HasKey(cp => cp.Id);

        // Diagnoses, goals and interventions are owned by the care plan; they
        // have no identity or lifecycle of their own.
        builder.OwnsMany(cp => cp.Diagnoses, d => d.ToTable("CarePlanDiagnoses", PatientsDbContext.Schema));
        builder.OwnsMany(cp => cp.Goals, g => g.ToTable("CarePlanGoals", PatientsDbContext.Schema));
        builder.OwnsMany(cp => cp.Intervention, i => i.ToTable("CarePlanInterventions", PatientsDbContext.Schema));
    }
}
