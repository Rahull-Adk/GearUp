using GearUp.Domain.Entities.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
    {
        public void Configure(EntityTypeBuilder<PostLike> builder)
        {
            builder.HasKey(pl => pl.Id);
            builder.HasQueryFilter(pl => pl.LikedUser != null && !pl.LikedUser.IsDeleted);

            builder.HasIndex(pl => new { pl.PostId, pl.LikedUserId }).IsUnique();
            builder.HasIndex(pl => new { pl.PostId, pl.UpdatedAt, pl.LikedUserId }).IsDescending(true, true, true);
        }
    }
}
