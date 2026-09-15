using Microsoft.EntityFrameworkCore;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Visitation.Infrastructure.Persistence;

internal sealed class VisitationDbContext(DbContextOptions<VisitationDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    public const string Schema = "Visitation";

    public DbSet<Visit> Visits => Set<Visit>();

    // The caller's care home. Referenced by the query filter so EF re-evaluates
    // it per request; a DbContext is scoped, so the care home is fixed for its lifetime.
    private CareHomeId CurrentCareHome => new(currentUser.CareHomeId);

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<VisitId>().HaveConversion<VisitTypedIdConverter>();
        builder.Properties<PatientId>().HaveConversion<VisitPatientTypedIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VisitationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Care home isolation: a caller can only ever read visits from their own
        // care home. Writes are stamped with the care home at booking time.
        modelBuilder.Entity<Visit>().HasQueryFilter(v => v.CareHomeId == CurrentCareHome);
    }
}
