using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Domain.Entities.Tokens;


namespace GearUp.Application.Services.Auth
{
    public class LogoutService : ILogoutService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        public LogoutService(IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
        }
        public async Task Logout(string refreshToken) { 
            var token = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken);
            RefreshToken.Revoke(token!);
            await _userRepository.SaveChangesAsync();
            
        }
    }
}
