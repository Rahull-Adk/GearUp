using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Domain.Entities.Tokens;


namespace GearUp.Application.Services.Auth
{
    public sealed class LogoutService : ILogoutService
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        public LogoutService(ITokenRepository tokenRepository, IUserRepository userRepository)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
        }
        public async Task<Result<string>> Logout(string refreshToken) { 
            var token = await _tokenRepository.GetRefreshTokenAsync(refreshToken);
            if (token == null)
            {
               return Result<string>.Failure("Invalid refresh token", 400);
            }
            token.Revoke();
            await _userRepository.SaveChangesAsync();
            return Result<string>.Success("Logout successful");
        }
    }
}
