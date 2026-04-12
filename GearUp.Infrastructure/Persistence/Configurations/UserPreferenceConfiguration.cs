using GearUp.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
    {
        public void Configure(EntityTypeBuilder<UserPreference> builder)
        {
            builder.HasKey(up => up.Id);
            builder.HasQueryFilter(up => up.User != null && !up.User.IsDeleted);

            builder.Property(up => up.CarMake).IsRequired().HasMaxLength(100);
            builder.Property(up => up.CarModel).IsRequired().HasMaxLength(100);
            builder.Property(up => up.CarColor).IsRequired().HasMaxLength(50);
            builder.Property(up => up.Price).IsRequired();
            builder.Property(up => up.CreatedAt).IsRequired();

            builder.HasOne(up => up.User)
                .WithMany(u => u.UserPreferences)
                .HasForeignKey(up => up.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

