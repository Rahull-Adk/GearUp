using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Domain.Entities.Tokens;


namespace GearUp.Application.Services.Auth
{
    public class LogoutService : ILogoutService
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        public LogoutService(ITokenRepository tokenRepository, IUserRepository userRepository)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
        }
        public async Task Logout(string refreshToken) { 
            var token = await _tokenRepository.GetRefreshTokenAsync(refreshToken);
            RefreshToken.Revoke(token!);
            await _userRepository.SaveChangesAsync();
            
        }
    }
}
