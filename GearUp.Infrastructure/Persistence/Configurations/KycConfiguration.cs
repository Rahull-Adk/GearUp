using GearUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GearUp.Infrastructure.Persistence.Configurations
{
    public class KycSubmissionsConfiguration : IEntityTypeConfiguration<KycSubmissions>
    {
        public void Configure(EntityTypeBuilder<KycSubmissions> builder)
        {
            builder.HasQueryFilter(k => !k.SubmittedBy.IsDeleted);

            builder.Property(k => k.Id).HasColumnType("char(36)")
                .UseCollation("utf8mb4_0900_ai_ci")
                .IsRequired();

            builder.Property(k => k.UserId)
                .HasColumnType("char(36)")
                .UseCollation("utf8mb4_0900_ai_ci")
                .IsRequired();

            builder.Property(k => k.ReviewerId)
                .HasColumnType("char(36)")
                .UseCollation("utf8mb4_0900_ai_ci")
                .IsRequired(false);

            // Relationships
            builder.HasOne(k => k.SubmittedBy)
                .WithMany(u => u.KycSubmitted)
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(k => k.ReviewedBy)
                .WithMany(u => u.KycSubmissionsReviewed)
                .HasForeignKey(k => k.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enums as ints
            builder.Property(k => k.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(k => k.DocumentType)
                .HasConversion<int>()
                .IsRequired();

            // Dates
            builder.Property(k => k.SubmittedAt)
                .IsRequired();

            builder.Property(k => k.VerifiedAt)
                .IsRequired(false);

            // Rejection reason
            builder.Property(k => k.RejectionReason)
                .HasMaxLength(500)
                .IsRequired(false);

        }
    }
}
