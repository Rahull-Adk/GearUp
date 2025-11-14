using System.Reflection.Emit;
using GearUp.Domain.Entities.Cars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class CarConfiguration : IEntityTypeConfiguration<Car>
    {
        public void Configure(EntityTypeBuilder<Car> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Description).IsRequired(false).HasMaxLength(1000);
            builder.Property(c => c.Make).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Model).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Color).IsRequired().HasMaxLength(50);
            builder.Property(c => c.VIN).HasMaxLength(50);
            builder.Property(c => c.LicensePlate).HasMaxLength(50);

            builder.HasOne(c => c.Dealer)
                .WithMany(u => u.Cars)
                .HasForeignKey(c => c.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
          
            builder.HasMany(c => c.Images)
                    .WithOne(ci => ci.Car)
                    .HasForeignKey(ci => ci.CarId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt).IsRequired();
        }
    }
}
