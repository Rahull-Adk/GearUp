using GearUp.Domain.Entities.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.Property(p => p.Caption).HasMaxLength(300);
            builder.Property(p => p.Content).IsRequired().HasMaxLength(2000);
            builder.Property(p => p.Visibility).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.UpdatedAt).IsRequired();

         
            builder.HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Car)
                .WithMany()
                .HasForeignKey(p => p.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Likes)
                .WithOne(l => l.Post)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Views)
                .WithOne(v => v.Post)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
