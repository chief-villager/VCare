using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCare.Modules.CarePlans.Domain.Entities;

namespace Vcare.Modules.CarePlans.Infrastructure.Persistence
{
    public class CarePlanDbContext(DbContextOptions<CarePlanDbContext> options) : DbContext(options)
    {
        public const string Schema = "CarePlan";

        public DbSet<CarePlan> CarePlans { get; set; } 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarePlanDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}