using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Modules.CareHomes.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Infrastructure.Configuration
{
    internal class CareHomeConfiguration : IEntityTypeConfiguration<Modules.CareHomes.Domain.Entity.CareHome>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CareHome> builder)
        {
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Address).IsRequired();
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Email).IsUnique();
        }

        
    }
}