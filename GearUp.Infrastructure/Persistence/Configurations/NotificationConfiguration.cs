using GearUp.Domain.Entities.RealTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            builder.HasQueryFilter(n => !n.ReceiverUser.IsDeleted);

            builder
                .HasOne(n => n.ReceiverUser)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.ReceiverUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(n => n.ActorUser)
                .WithMany(u => u.NotificationsTriggered)
                .HasForeignKey(n => n.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
