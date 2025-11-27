using GearUp.Domain.Entities.RealTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{


    public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            builder.HasKey(cp => new { cp.UserId, cp.ConversationId });
            builder.HasQueryFilter(cp => !cp.User.IsDeleted);

            builder.Property(cp => cp.JoinedAt)
                .IsRequired();

            builder.Property(cp => cp.IsMuted)
                .IsRequired();

            builder.HasOne(cp => cp.User)
                .WithMany(u => u.ConversationParticipants)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
