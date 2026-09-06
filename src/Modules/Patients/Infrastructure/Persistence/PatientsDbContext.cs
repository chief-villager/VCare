using Microsoft.EntityFrameworkCore;
using Patients.Infrastructure.Persistence;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Persistence;

internal sealed class PatientsDbContext(DbContextOptions<PatientsDbContext> options) : DbContext(options),IUnitOfWork
{
    public const string Schema = "patients";

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<PatientId>().HaveConversion<PatientTypedIConverter>();
        builder.Properties<CarePlanId>().HaveConversion<CarePlanTypedIdConverter>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
