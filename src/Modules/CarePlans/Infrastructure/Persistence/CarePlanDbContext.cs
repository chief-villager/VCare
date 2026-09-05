using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarePlans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace Vcare.Modules.CarePlans.Infrastructure.Persistence
{
    internal class CarePlanDbContext(DbContextOptions<CarePlanDbContext> options) : DbContext(options), IUnitOfWork
    {
        public const string Schema = "CarePlan";

        public DbSet<CarePlan> CarePlans { get; set; } 

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<CarePlanId>().HaveConversion<CarePlanTypedIdConverter>();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarePlanDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}