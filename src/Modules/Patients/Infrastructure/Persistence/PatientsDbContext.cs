using VCare.SharedKernel.Auditing;
using Microsoft.EntityFrameworkCore;
using Patients.Infrastructure.Persistence;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Persistence;

internal sealed class PatientsDbContext(DbContextOptions<PatientsDbContext> options, ICurrentUser currentUser) : DbContext(options),IUnitOfWork
{
    public const string Schema = "patients";

    public DbSet<Patient> Patients => Set<Patient>();

    private CareHomeId CurrentCareHome => new(currentUser.CareHomeId); 

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<PatientId>().HaveConversion<PatientTypedIConverter>();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        // Audit rows are written in the same save as the change (see AuditSaveChangesInterceptor).
        modelBuilder.MapAuditEntries();
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Patient>().HasQueryFilter(p => p.CareHomeId == CurrentCareHome);

    }
}
