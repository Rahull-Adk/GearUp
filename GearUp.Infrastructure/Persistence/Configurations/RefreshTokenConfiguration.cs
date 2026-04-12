using GearUp.Domain.Entities.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);
            builder.Property(rt => rt.Token).HasMaxLength(64).IsRequired();
            builder.HasIndex(rt => rt.Token).IsUnique();
            builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt });
            builder.HasQueryFilter(rt => rt.User != null && !rt.User.IsDeleted);
        }
    }
}
