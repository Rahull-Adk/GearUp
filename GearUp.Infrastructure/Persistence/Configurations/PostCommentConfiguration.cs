using GearUp.Domain.Entities.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
    {
        public void Configure(EntityTypeBuilder<PostComment> builder)
        {

            builder.HasKey(pc => pc.Id);

            builder.UseCollation("utf8mb4_general_ci");

            builder.HasQueryFilter(pc => !pc.IsDeleted);

            builder.Property(pc => pc.Content)
                .IsRequired();

            // Composite index for top-level comments (PostId, ParentCommentId = null, CreatedAt)
            // and child comments (ParentCommentId, CreatedAt)
            builder.HasIndex(pc => new { pc.PostId, pc.ParentCommentId, pc.CreatedAt });
        }
    }
}
