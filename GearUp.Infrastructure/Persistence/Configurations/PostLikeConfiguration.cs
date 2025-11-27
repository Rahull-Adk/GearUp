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
            builder.HasQueryFilter(pl => !pl.LikedUser.IsDeleted);
        }
    }
}
