using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using Medications.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;

namespace Medications.Infrastructure.Persistence
{
    internal sealed class MedicationDbContext : DbContext,IUnitOfWork
    {
        internal const string Schema = "Medication";
        internal DbSet<MedicationOrder> MedicationOrders{ get; set;}
        internal DbSet<MedicationAdministration> MedicationAdministrations{get; set;}
       
        internal MedicationDbContext(DbContextOptions<MedicationDbContext> options)
        : base(options)
        {
            
        }
       
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<MedicationOrderId>().HaveConversion<MedicationOrderIdConverter>();
            builder.Properties<MedicationAdministrationId>().HaveConversion<MedicationAdministrationIdConverter>();
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfiguration(new MedicationOrderConfiguration());
            base.OnModelCreating(modelBuilder);
        }
      
    }
}