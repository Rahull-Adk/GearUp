using GearUp.Domain.Entities.Users;
namespace GearUp.Domain.Entities.Tokens
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public Guid UserId { get; private set; }
        public User? User { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public RefreshToken()
        {
            
        }

        public static RefreshToken CreateRefreshToken(string token, DateTime expiresAt, Guid userId)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                ExpiresAt = expiresAt,
                UserId = userId,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Revoke()
        {
            this.IsRevoked = true;
        }
    }
}
