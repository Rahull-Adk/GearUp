using GearUp.Domain.Entities.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.HasKey(prt => prt.Id);
            builder.Property(prt => prt.Token).HasMaxLength(64).IsRequired();
            builder.HasIndex(prt => prt.Token).IsUnique();
            builder.HasIndex(prt => new { prt.UserId, prt.IsUsed, prt.ExpiresAt });
            builder.HasQueryFilter(prt => prt.User != null && !prt.User.IsDeleted);
        }
    }
}
