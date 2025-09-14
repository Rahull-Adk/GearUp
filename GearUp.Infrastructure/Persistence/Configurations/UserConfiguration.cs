using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Provider).IsRequired(false).HasMaxLength(50);
            builder.Property(u => u.ProviderUserId).IsRequired(false).HasMaxLength(100);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.Role).IsRequired();
            builder.Property(u => u.PhoneNumber).HasMaxLength(15);
            builder.Property(u => u.AvatarUrl).IsRequired().HasMaxLength(200);
            builder.Property(u => u.IsEmailVerified).IsRequired();
            builder.Property(u => u.IsProfileCompleted).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();

            //Tokens

            builder.HasMany<RefreshToken>()
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany<EmailVerificationToken>()
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany<PasswordResetToken>()
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rentals

            builder.HasMany(u => u.OwnedRentals)
                .WithOne(r => r.Tenant)
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.BookedRentals)
                .WithOne(r => r.Renter)
                .HasForeignKey(r => r.RenterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointments

            builder.HasMany(u => u.ReceivedAppointments)
                .WithOne(a => a.Agent)
                .HasForeignKey(a => a.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.SentAppointments)
                .WithOne(a => a.Requester)
                .HasForeignKey(a => a.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Messages and Conversations

            builder.HasMany(u => u.ConversationParticipants)
               .WithOne(cp => cp.User)
               .HasForeignKey(cp => cp.UserId)
               .OnDelete(DeleteBehavior.Cascade);


            //  Notifications
            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
