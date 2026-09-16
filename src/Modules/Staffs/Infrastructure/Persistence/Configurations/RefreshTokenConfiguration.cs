using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Staffs.Domain.Entity;

namespace Staffs.Infrastructure.Persistence.Configurations
{
    internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(r => r.Id);

            // Every lookup is by hash, and two tokens must never share one.
            builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(r => r.TokenHash).IsUnique();

            // The reuse sweep revokes a whole family at once.
            builder.HasIndex(r => r.TokenFamily);

            builder.Property(r => r.TokenFamily).IsRequired();
            builder.Property(r => r.CreatedTime).IsRequired();
            builder.Property(r => r.ExpirationDate).IsRequired();
            builder.Property(r => r.IsRevoked).IsRequired();
        }
    }
}
