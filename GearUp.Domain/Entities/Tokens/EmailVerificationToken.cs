using GearUp.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearUp.Domain.Entities.Tokens
{
    public class EmailVerificationToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public EmailVerificationToken()
        {

        }

        public static EmailVerificationToken CreateEmailVerificationToken(string token, DateTime expiresAt, Guid userId)
        {
            return new EmailVerificationToken
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
