using GearUp.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class UserReviewConfiguration : IEntityTypeConfiguration<UserReview>
    {
        public void Configure(EntityTypeBuilder<UserReview> builder)
        {
            builder.HasKey(ur => ur.Id);
            builder.HasQueryFilter(ur => !ur.Reviewer.IsDeleted && !ur.Reviewee.IsDeleted);

            builder.Property(ur => ur.ReviewText)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(ur => ur.Rating)
                .IsRequired();

            builder.Property(ur => ur.CreatedAt)
                .IsRequired();

            builder.Property(ur => ur.UpdatedAt)
                .IsRequired();

            builder.HasOne(ur => ur.Reviewer)
                .WithMany()
                .HasForeignKey(ur => ur.ReviewerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(ur => ur.Reviewee)
                .WithMany()
                .HasForeignKey(ur => ur.RevieweeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Ensure one review per dealer per customer
            builder.HasIndex(ur => new { ur.ReviewerId, ur.RevieweeId })
                .IsUnique();
        }
    }
}
