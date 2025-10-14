
using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Tokens;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly GearUpDbContext _db;
        public RefreshTokenRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _db.RefreshTokens.AddAsync(refreshToken);
        }
        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        }
    }
}
