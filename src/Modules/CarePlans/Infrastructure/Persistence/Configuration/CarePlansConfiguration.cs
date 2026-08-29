using Microsoft.EntityFrameworkCore;
using VCare.Modules.CarePlans.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vcare.Modules.CarePlans.Infrastructure.Persistence;

namespace VCare.Modules.CarePlans.Infrastructure.Persistence.Configuration
{
    public class CarePlansConfiguration : IEntityTypeConfiguration<CarePlan>
    {
        public void Configure(EntityTypeBuilder<CarePlan> builder)
        {
            builder.ToTable("CarePlans", CarePlanDbContext.Schema);
            builder.HasKey(cp => cp.Id);
            builder.Ignore(p => p.DomainEvents);
        }
    }
}