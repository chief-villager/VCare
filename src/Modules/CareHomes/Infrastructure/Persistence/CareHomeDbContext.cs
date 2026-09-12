using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using src.Modules.CareHomes.Domain.Entity;
using src.Modules.CareHomes.Infrastructure.Configuration;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Infrastructure.Persistence
{
    internal sealed class CareHomeDbContext(DbContextOptions<CareHomeDbContext> options) : DbContext(options)
    {
        public const string SchemaName = "CareHomes";
        public DbSet<CareHome> CareHomes{get; set;}

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<CareHomeId>().HaveConversion<CareHomeIdConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CareHomeDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
