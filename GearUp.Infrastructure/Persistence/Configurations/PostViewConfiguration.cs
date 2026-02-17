using GearUp.Domain.Entities.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class PostViewConfiguration : IEntityTypeConfiguration<PostView>
    {
        public void Configure(EntityTypeBuilder<PostView> builder)
        {
            builder.HasKey(pv => pv.Id);
            builder.HasQueryFilter(pv => !pv.Post.IsDeleted);

            // Composite index for view existence checks (HasViewTimeElapsedAsync) and counting
            builder.HasIndex(pv => new { pv.PostId, pv.ViewedUserId, pv.ViewedAt });
        }
    }
}
