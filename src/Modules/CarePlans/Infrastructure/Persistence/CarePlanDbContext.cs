using Microsoft.EntityFrameworkCore;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Infrastructure.Persistence;

internal sealed class CarePlanDbContext(DbContextOptions<CarePlanDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    public const string Schema = "CarePlan";

    public DbSet<CarePlan> CarePlans => Set<CarePlan>();

    // The caller's care home. Referenced by the query filter so EF re-evaluates
    // it per request; a DbContext is scoped, so the care home is fixed for its lifetime.
    private CareHomeId CurrentCareHome => new(currentUser.CareHomeId);

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<CarePlanId>().HaveConversion<CarePlanTypedIdConverter>();
        builder.Properties<PatientId>().HaveConversion<PatientTypedIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarePlanDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Care home isolation: a caller can only ever read care plans from their
        // own care home. Writes are stamped with the care home at creation time.
        modelBuilder.Entity<CarePlan>().HasQueryFilter(c => c.CareHomeId == CurrentCareHome);
    }
}
