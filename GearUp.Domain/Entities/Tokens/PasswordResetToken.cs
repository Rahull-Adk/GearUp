using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Tokens
{
    public class PasswordResetToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        public Guid UserId { get; private set; }
        public User? User { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public PasswordResetToken()
        {

        }

        public static PasswordResetToken CreatePasswordResetToken(string token, DateTime expiresAt, Guid userId)
        {
            return new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                ExpiresAt = expiresAt,
                IsUsed = false,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
