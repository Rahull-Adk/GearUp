using GearUp.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearUp.Domain.Entities.Tokens
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; }
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
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
