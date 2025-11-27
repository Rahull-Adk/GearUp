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
        }
    }
}
