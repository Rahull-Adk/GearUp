using GearUp.Domain.Entities.Cars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Location)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.Notes)
                .HasMaxLength(1000);

            builder.Property(a => a.Status)
                .IsRequired();

            builder.Property(a => a.Schedule)
                .IsRequired();

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.UpdatedAt)
                .IsRequired();

            builder.HasOne(a => a.Agent)
                .WithMany(u => u.ReceivedAppointments)
                .HasForeignKey(a => a.AgentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(a => a.Requester)
                .WithMany(u => u.SentAppointments)
                .HasForeignKey(a => a.RequesterId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
