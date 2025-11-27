using GearUp.Domain.Entities.Cars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class CarImageConfiguration : IEntityTypeConfiguration<CarImage>
    {
        public void Configure(EntityTypeBuilder<CarImage> builder)
        {
            builder.HasKey(ci => ci.Id);
            builder.HasQueryFilter(ci => !ci.Car.IsDeleted);
        }
    }
}
