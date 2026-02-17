using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
     .HasColumnType("char(36)")
     .UseCollation("utf8mb4_0900_ai_ci")
     .IsRequired();

            builder.Property(cl => cl.CommentId)
                   .HasColumnType("char(36)")
                   .UseCollation("utf8mb4_0900_ai_ci")
                   .IsRequired();

            builder.Property(cl => cl.LikedUserId)
                   .HasColumnType("char(36)")
                   .UseCollation("utf8mb4_0900_ai_ci")
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
