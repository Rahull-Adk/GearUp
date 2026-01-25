using GearUp.Domain.Entities.RealTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Text)
                .HasMaxLength(2000);

            builder.Property(m => m.ImageUrl)
                .HasMaxLength(500);

            builder.HasIndex(m => m.ConversationId);
            builder.HasIndex(m => m.SentAt);

            builder.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(m => !m.Sender!.IsDeleted);
        }
    }
}
