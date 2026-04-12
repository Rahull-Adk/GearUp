using GearUp.Domain.Entities.Posts;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CommentLike> builder)
        {


            builder.HasKey(cl => cl.Id);

            builder.Property(cl => cl.Id)
     .IsRequired();

            builder.Property(cl => cl.CommentId)
                   .IsRequired();

            builder.Property(cl => cl.LikedUserId)
                   .IsRequired();

            builder.HasQueryFilter(cl => !cl.LikedUser.IsDeleted);

            builder.HasOne(cl => cl.Comment)
                   .WithMany(c => c.Likes)
                   .HasForeignKey(cl => cl.CommentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint to prevent duplicate likes + efficient lookup
            builder.HasIndex(cl => new { cl.CommentId, cl.LikedUserId }).IsUnique();

        }

    }
}
