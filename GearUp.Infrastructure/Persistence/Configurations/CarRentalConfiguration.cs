using GearUp.Domain.Entities.Cars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class CarRentalConfiguration : IEntityTypeConfiguration<CarRental>
    {
        public void Configure(EntityTypeBuilder<CarRental> builder)
        {
            builder.HasKey(cr => cr.Id);
            builder.HasQueryFilter(cr => !cr.Car.IsDeleted);
        }
    }
}
