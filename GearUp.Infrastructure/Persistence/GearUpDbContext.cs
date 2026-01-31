using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;


namespace GearUp.Infrastructure.Persistence
{
    public class GearUpDbContext : DbContext
    {

        public GearUpDbContext(DbContextOptions<GearUpDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<UserReview> UserReviews { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<KycSubmissions> KycSubmissions { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<CarImage> CarImages { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<PostView> PostViews { get; set; }

        public DbSet<CommentLike> CommentLikes { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(GearUpDbContext).Assembly);


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(Guid) || p.ClrType == typeof(Guid?)))
                {
                    property.SetColumnType("char(36)");
                    property.SetAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");
                }
            }
        }

    }
}
