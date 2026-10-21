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
            builder.HasIndex(x => x.CareHomeId);
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
            builder.HasData([
                new{
                    Id = Guid.Parse("f3c9d1e5-6078-4901-c345-d678e901f234"),
                    Name = "Refused",
                    DisplayLetter = "R"

                },
                new {
                    Id = Guid.Parse("e2b8c0d4-5f67-4890-b234-c567d890e123"),
                    Name = "Taken",
                    DisplayLetter = "T",
                },
                new {
                    Id = Guid.Parse("db5662a7-5f67-4890-841f-c567d890e123"),
                    Name = "Missed",
                    DisplayLetter = "M",
                },

            ]);
        }
    }

}
