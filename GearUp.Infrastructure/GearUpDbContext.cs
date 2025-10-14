using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Chats;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure
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

        public DbSet<Car> Cars { get; set; }
        public DbSet<CarRental> CarRentals { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<Post> Posts { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<PostView> PostViews { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(GearUpDbContext).Assembly);
        }

    }
}
