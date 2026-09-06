using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Staffs.Domain.Entity;

namespace Staffs.Infrastructure.Persistence.Configurations
{
    internal class StaffConfiguration : IEntityTypeConfiguration<Staffs.Domain.Entity.Staff>
    {
        public void Configure(EntityTypeBuilder<Staffs.Domain.Entity.Staff> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Email).IsRequired().HasMaxLength(200);
            builder.Property(s => s.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(s => s.Role).IsRequired().HasMaxLength(50);
        }

       
    }
   
}