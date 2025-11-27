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
            builder.HasQueryFilter(pc => !pc.IsDeleted && !pc.CommentedUser.IsDeleted);
        }
    }
}
