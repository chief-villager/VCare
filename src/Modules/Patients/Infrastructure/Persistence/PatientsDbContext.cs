using Microsoft.EntityFrameworkCore;
using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Infrastructure.Persistence;

public sealed class PatientsDbContext(DbContextOptions<PatientsDbContext> options) : DbContext(options)
{
    public const string Schema = "patients";

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
