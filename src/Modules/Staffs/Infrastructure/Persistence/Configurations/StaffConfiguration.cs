using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Staffs.Domain;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Persistence.Configurations
{
    internal class StaffConfiguration : IEntityTypeConfiguration<Staffs.Domain.Entity.Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Email).IsRequired().HasMaxLength(200);
            builder.Property(s => s.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(s => s.Role).IsRequired().HasMaxLength(50);
            builder.Property(s => s.CareHomeId)
                .HasConversion(id => id.Value, value => new CareHomeId(value))
                .IsRequired();
            builder.HasIndex(s => s.CareHomeId);
            builder.HasIndex(s => s.UserName).IsUnique();
        }

       
    }

   internal class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        private static readonly Guid AdminRoleId = new("11111111-1111-1111-1111-111111111111");
        private static readonly Guid CarerRoleId = new("22222222-2222-2222-2222-222222222222");
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.HasData(
                new ApplicationRole
                {
                    Id = AdminRoleId,
                    Name = nameof(RoleEnum.Admin),
                    NormalizedName = nameof(RoleEnum.Admin).ToUpper()
                    
                },
                new IdentityRole<Guid>
                {
                    Id = CarerRoleId,
                    Name = nameof(RoleEnum.Carer),
                    NormalizedName = nameof(RoleEnum.Carer).ToUpper()
                }
            );
        }
    }
}