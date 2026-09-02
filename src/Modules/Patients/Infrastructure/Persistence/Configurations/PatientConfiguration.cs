using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", PatientsDbContext.Schema);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();

        // Domain events are behaviour, not persisted state.
        builder.Ignore(p => p.DomainEvents);
    }
}
