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
            builder.HasQueryFilter(u => !u.IsDeleted);
            builder.Property(u => u.Id)
     .HasColumnType("char(36)")
     .UseCollation("utf8mb4_0900_ai_ci")
     .IsRequired();
            builder.Property(u => u.Provider).IsRequired(false).HasMaxLength(50);
            builder.Property(u => u.ProviderUserId).IsRequired(false).HasMaxLength(100);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.Role).IsRequired();
            builder.Property(u => u.PhoneNumber).HasMaxLength(15);
            builder.Property(u => u.PendingEmail).HasMaxLength(100);
            builder.Property(u => u.AvatarUrl).IsRequired().HasMaxLength(200);
            builder.Property(u => u.IsEmailVerified).IsRequired();
            builder.Property(u => u.IsPendingEmailVerified).IsRequired().HasDefaultValue(false);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();

            //Tokens

            builder.HasMany<RefreshToken>()
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
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


            // Submitted KYCs
            builder.HasMany(u => u.KycSubmitted)
                .WithOne(k => k.SubmittedBy)
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reviewed KYCs
            builder.HasMany(u => u.KycSubmissionsReviewed)
                .WithOne(k => k.ReviewedBy)
                .HasForeignKey(k => k.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Preferences
            builder.HasMany(u => u.UserPreferences)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
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
                .WithOne(n => n.ReceiverUser)
                .HasForeignKey(n => n.ReceiverUserId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
