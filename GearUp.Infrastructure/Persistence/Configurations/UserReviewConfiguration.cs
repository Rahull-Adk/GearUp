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
        }
    }
}
