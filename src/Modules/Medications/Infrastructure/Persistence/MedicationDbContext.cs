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
        internal DbSet<OutcomeCode> Outcomes{get; set;}
       
        private readonly ICurrentUser _currentUser;

        public MedicationDbContext(DbContextOptions<MedicationDbContext> options, ICurrentUser currentUser)
        : base(options)
        {
            _currentUser = currentUser;
        }

        // The caller's care home. Referenced by the query filter so EF re-evaluates
        // it per request; a DbContext is scoped, so the care home is fixed for its lifetime.
        private CareHomeId CurrentCareHome => new(_currentUser.CareHomeId);
       
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<MedicationOrderId>().HaveConversion<MedicationOrderIdConverter>();
            builder.Properties<MedicationAdministrationId>().HaveConversion<MedicationAdministrationIdConverter>();
            builder.Properties<CareHomeId>().HaveConversion<CareHomeIdConverter>();
            builder.Properties<PatientId>().HaveConversion<PatientIdConverter>();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);

            // Care home isolation: a caller can only ever read medication orders and
            // administrations from their own care home. Writes are stamped with the
            // care home at creation time. Outcome codes are shared reference data and
            // stay unfiltered.
            modelBuilder.Entity<MedicationOrder>().HasQueryFilter(o => o.CareHomeId == CurrentCareHome);
            modelBuilder.Entity<MedicationAdministration>().HasQueryFilter(a => a.CareHomeId == CurrentCareHome);
        }
      
    }
}