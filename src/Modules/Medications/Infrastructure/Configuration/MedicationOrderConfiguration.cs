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
            builder.Ignore(x => x.DomainEvents);
            //converts every enum to string on the database
            builder.Property(m => m.Route).HasConversion<string>().HasMaxLength(30);
            builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(30);
            // Every read is filtered by care home, so it leads the index.
            builder.HasIndex(x => x.CareHomeId);
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
            builder.Property(x => x.Reason).HasMaxLength(MedicationAdministration.ReasonMaxLength);
            builder.Property(x => x.DoseGiven).HasMaxLength(MedicationAdministration.DoseGivenMaxLength);
            builder.HasIndex(x => x.CareHomeId);
            // One record per scheduled slot. Sign-off and the missed-dose recorder
            // can race for the same slot; the database, not a prior read, decides
            // who wins. PRN doses have no slot, so they are left out.
            builder.HasIndex(x => new { x.MedicationOrderId, x.ScheduledFor })
                .IsUnique()
                .HasFilter("[ScheduledFor] IS NOT NULL");
        }
    }

    internal class DoseScheduleConfiguration : IEntityTypeConfiguration<DoseSchedule>
    {
        public void Configure(EntityTypeBuilder<DoseSchedule> builder)
        {
            builder.ToTable("DoseSchedule", MedicationDbContext.Schema);
            builder.HasKey(x => x.Id);
            builder.Property( x => x.FType).HasConversion<string>().HasMaxLength(30);
            // Flags are stored by name, so all seven days ("Mon, Tue, ..., Sun") is 33 characters.
            builder.Property( x => x.DaysOfWeek).HasConversion<string>().HasMaxLength(50);
            
        }
       
    }

    internal class OutcomeCodeConfiguration : IEntityTypeConfiguration<OutcomeCode>
    {
        public void Configure(EntityTypeBuilder<OutcomeCode> builder)
        {
            builder.ToTable("OutcomeCode", MedicationDbContext.Schema);
            builder.HasKey( x => x.Id);
            // The ids are fixed: sign-off and the MAR chart recognise Taken and
            // Missed by id, so the rows must never be reseeded under new ones.
            builder.HasData(OutcomeCode.Seeded);
        }
    }

}
