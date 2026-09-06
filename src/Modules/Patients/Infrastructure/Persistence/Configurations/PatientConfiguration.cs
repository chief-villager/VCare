using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", PatientsDbContext.Schema);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Firstname).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Lastname).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Gender).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DateOfBirth).IsRequired();

        // Care plans live inside the Patient aggregate boundary.
        builder.HasMany(p => p.CarePlans)
            .WithOne()
            .HasForeignKey(cp => cp.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.CarePlans).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Domain events are behaviour, not persisted state.
        builder.Ignore(p => p.DomainEvents);
    }
}
