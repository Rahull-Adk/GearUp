
using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Tokens;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private readonly GearUpDbContext _db;
        public TokenRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddPasswordResetTokenAsync(PasswordResetToken passwordResetToken)
        {
            await _db.PasswordResetTokens.AddAsync(passwordResetToken);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _db.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
        {
            return await _db.PasswordResetTokens.FirstOrDefaultAsync(pt => pt.Token == token);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        }

    }
}
