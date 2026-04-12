using GearUp.Domain.Entities.Tokens;
namespace GearUp.Application.Interfaces.Repositories
{
    public interface ITokenRepository
    {
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash);
        Task AddPasswordResetTokenAsync(PasswordResetToken passwordResetToken);
        Task<PasswordResetToken?> GetPasswordResetTokenAsync(string tokenHash);
    }
}
