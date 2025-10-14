using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace GearUp.Application.Services.Auth
{
    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        public LoginService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher<User> passwordHasher, ITokenGenerator tokenGenerator)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }
        public async Task<Result<LoginResponseDto>> LoginUser(LoginRequestDto req)
        {
            Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            var user = emailRegex.IsMatch(req.UsernameOrEmail)
                ? await _userRepository.GetUserByEmailAsync(req.UsernameOrEmail)
                : await _userRepository.GetUserByUsernameAsync(req.UsernameOrEmail);
            if (user == null)
            {
                return Result<LoginResponseDto>.Failure("User not found", 404);
            }
            var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                return Result<LoginResponseDto>.Failure("Invalid Credentials", 401);
            }
            if(!user.IsEmailVerified)
            {
                return Result<LoginResponseDto>.Failure("Email not verified. Please verify your email to login.", 403);
            }
            var accessClaims = new[]
                {
                   new Claim("id", user.Id.ToString()),
                   new Claim("email", user.Email),
                   new Claim("role", user.Role.ToString()),
                   new Claim("purpose", "access_token")
                };

            var accessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();
            DateTime expiresAt = DateTime.UtcNow.AddDays(7);

            var refreshTokenEntity = RefreshToken.CreateRefreshToken(refreshToken, expiresAt, user.Id);
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            await _userRepository.SaveChangesAsync();


            return Result<LoginResponseDto>.Success(new LoginResponseDto { AccessToken = accessToken, RefreshToken = refreshToken }, "User logged in successfully!", 200);
        }

        public async Task<Result<LoginResponseDto>> RotateRefreshToken(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Result<LoginResponseDto>.Failure("Refresh token is missing", 401);
            }
            var storedToken = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Result<LoginResponseDto>.Failure("Invalid or expired refresh token", 401);
            }
            var user = await _userRepository.GetUserByIdAsync(storedToken.UserId);
            if (user == null)
            {
                return Result<LoginResponseDto>.Failure("User not found", 404);
            }
            var accessClaims = new[]
               {
                   new Claim("id", user.Id.ToString()),
                   new Claim("email", user.Email),
                   new Claim("role", user.Role.ToString()),
                   new Claim("purpose", "access_token")
            };
            var newAccessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
            RefreshToken.CreateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7), user.Id);
            RefreshToken.Revoke(storedToken);
            await _userRepository.SaveChangesAsync();
            return Result<LoginResponseDto>.Success(new LoginResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken}, "Token Rotated Successfully", 200);
        }
    }
}
