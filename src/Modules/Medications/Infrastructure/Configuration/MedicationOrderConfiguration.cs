using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using Medications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Medications.Infrastructure.Configuration
{
    internal class MedicationOrderConfiguration : IEntityTypeConfiguration<MedicationOrder>
    {
        public void Configure(EntityTypeBuilder<MedicationOrder> builder)
        {
            builder.ToTable("MedicationOrder", MedicationDbContext.Schema);
            builder.HasKey(x => x.Id);
            builder.Ignore("domainEvent");
            builder.HasMany(x => x.Administrations).WithOne().HasForeignKey(x => x.MedicationOrderId).OnDelete(DeleteBehavior.ClientNoAction);
            builder.HasMany(x => x.Schedule).WithOne().HasForeignKey(x => x.MedicationOrderId).OnDelete(DeleteBehavior.ClientNoAction);

        }
    }

    internal class MedicationAdminstrationConfiguration : IEntityTypeConfiguration<MedicationAdministration>
    {
        public void Configure(EntityTypeBuilder<MedicationAdministration> builder)
        {
            builder.ToTable("MedicationAdminstration", MedicationDbContext.Schema);
            builder.HasKey(x => x.Id);
        }
    }

    internal class DoseScheduleConfiguration : IEntityTypeConfiguration<DoseSchedule>
    {
        public void Configure(EntityTypeBuilder<DoseSchedule> builder)
        {
            builder.ToTable("DoseSchedule", MedicationDbContext.Schema);
            builder.HasKey(x => x.Id);
            
        }
       
    }

    internal class OutcomeCodeConfiguration : IEntityTypeConfiguration<OutcomeCode>
    {
        public void Configure(EntityTypeBuilder<OutcomeCode> builder)
        {
            builder.ToTable("OutcomeCode", MedicationDbContext.Schema);
            builder.HasKey( x => x.Id);
        }
    }

}
