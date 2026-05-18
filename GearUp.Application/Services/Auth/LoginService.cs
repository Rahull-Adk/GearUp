using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Diagnostics;
using GearUp.Application.Messaging.Contracts;

namespace GearUp.Application.Services.Auth
{
    public sealed partial class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<AdminLoginRequestDto> _adminLoginValidator;
        private readonly IValidator<PasswordResetReqDto> _passwordResetValidator;
        private readonly ILogger<LoginService> _logger;

        public LoginService(IUserRepository userRepository, ITokenRepository tokenRepository,
            IPasswordHasher<User> passwordHasher, ITokenGenerator tokenGenerator, IMessagePublisher messagePublisher,
            IValidator<LoginRequestDto> loginValidator, IValidator<AdminLoginRequestDto> adminLoginValidator,
            IValidator<PasswordResetReqDto> passwordResetValidator, ILogger<LoginService> logger)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _messagePublisher = messagePublisher;
            _loginValidator = loginValidator;
            _adminLoginValidator = adminLoginValidator;
            _passwordResetValidator = passwordResetValidator;
            _logger = logger;
        }

        private static readonly ActivitySource _activitySource =
            new("GearUp.Auth");

        public async Task<Result<LoginResponseDto>> LoginUser(LoginRequestDto req)
        {
            using var activity = _activitySource.StartActivity("Login");
            _logger.LogInformation("Attempting to log in user with identifier: {Identifier}", req.UsernameOrEmail);
            using (_activitySource.StartActivity("Validations"))
            {
                await _loginValidator.EnsureValidAsync(req);
            }

            Regex emailRegex = MyRegex();
            var user = emailRegex.IsMatch(req.UsernameOrEmail)
                ? await _userRepository.GetUserEntityByEmailAsync(req.UsernameOrEmail)
                : await _userRepository.GetUserEntityByUsernameAsync(req.UsernameOrEmail);
            
            if (user is null || user.Role == UserRole.Admin)
                throw new NotFoundException("User not Found");

            return await HandleLogin(user, req.Password);
        }

        public async Task<Result<LoginResponseDto>> LoginAdmin(AdminLoginRequestDto req)
        {
            _logger.LogInformation("Attempting to log in admin with email: {Email}", req.Email);
            await _adminLoginValidator.EnsureValidAsync(req);

            var user = await _userRepository.GetUserEntityByEmailAsync(req.Email);
            if (user?.Role != UserRole.Admin)
                throw new NotFoundException("Admin not found.");

            return await HandleLogin(user, req.Password);
        }


        private async Task<Result<LoginResponseDto>> HandleLogin(User? user, string password)
        {
            if (user == null)
                throw new NotFoundException("User not found");

            using (_activitySource.StartActivity("VerifyHashedPassword"))
            {
                var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);


                if (passwordVerification == PasswordVerificationResult.Failed)
                    throw new UnauthorizedException("Invalid Credentials");
            }

            if (user.Role != UserRole.Admin && !user.IsEmailVerified)
                throw new ForbiddenException("Email not verified. Please verify your email to login.");

            var accessToken = string.Empty;
            var refreshToken = string.Empty;


            using (_activitySource.StartActivity("Token Generations"))
            {
                var accessClaims = new[]
                {
                    new Claim("id", user.Id.ToString()), new Claim("email", user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("purpose", "access_token")
                };

                accessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
                refreshToken = _tokenGenerator.GenerateRefreshToken();
            }

            var expiresAt = DateTime.UtcNow.AddDays(7);
            var refreshTokenHash = _tokenGenerator.HashOpaqueToken(refreshToken);
            var refreshTokenEntity = RefreshToken.CreateRefreshToken(refreshTokenHash, expiresAt, user.Id);
            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<LoginResponseDto>.Success(
                new LoginResponseDto { AccessToken = accessToken, RefreshToken = refreshToken },
                "Logged in successfully!",
                200
            );
        }


        public async Task<Result<LoginResponseDto>> RotateRefreshToken(string refreshToken)
        {
            _logger.LogInformation("Attempting to rotate refresh token.");

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token is missing.");
                throw new UnauthorizedException("Refresh token is missing");
            }

            var refreshTokenHash = _tokenGenerator.HashOpaqueToken(refreshToken);
            var storedToken = await _tokenRepository.GetRefreshTokenAsync(refreshTokenHash);
            if (storedToken is null)
            {
                // Transitional fallback: if legacy plaintext rows exist, upgrade them to hash on successful use.
                storedToken = await _tokenRepository.GetRefreshTokenAsync(refreshToken);
                if (storedToken is not null)
                {
                    storedToken.SetTokenHash(refreshTokenHash);
                }
            }
            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            // Use GetUserEntityByIdAsync to retrieve the actual User entity (test and implementation provide this)
            var user = await _userRepository.GetUserEntityByIdAsync(storedToken.UserId)
                       ?? throw new NotFoundException("User not found");

            var accessClaims = new[]
            {
                new Claim("id", user.Id.ToString()), new Claim("email", user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("purpose", "access_token")
            };
            var newAccessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenGenerator.HashOpaqueToken(newRefreshToken);
            var refreshTokenEntity =
                RefreshToken.CreateRefreshToken(newRefreshTokenHash, DateTime.UtcNow.AddDays(7), user.Id);
            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            storedToken.Revoke();
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", user.Id);
            return Result<LoginResponseDto>.Success(
                new LoginResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken },
                "Token Rotated Successfully", 200);
        }

        public async Task<Result<string>> SendPasswordResetToken(string email)
        {
            _logger.LogInformation("Attempting to send password reset token to email: {Email}", email);
            Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (emailRegex.IsMatch(email) == false)
            {
                throw new Domain.Exceptions.ValidationException("Invalid email format");
            }

            var user = await _userRepository.GetUserEntityByEmailAsync(email)
                       ?? throw new NotFoundException("User not found");

            var token = _tokenGenerator.GeneratePasswordResetToken();
            var tokenHash = _tokenGenerator.HashOpaqueToken(token);

            var passwordRefreshTokenEntity =
                PasswordResetToken.CreatePasswordResetToken(tokenHash, DateTime.UtcNow.AddHours(1), user.Id);
            await _tokenRepository.AddPasswordResetTokenAsync(passwordRefreshTokenEntity);
            await _userRepository.SaveChangesAsync();

            var emailMessage = new EmailRequestMessage
            {
                CorrelationId = user.Id.ToString(),
                ToEmail = user.Email,
                TemplateName = "ResetPassword",
                Payload = new Dictionary<string, string>
                {
                    ["token"] = token
                }
            };

            await _messagePublisher.PublishAsync(emailMessage, "gearup.email.queue");

            _logger.LogInformation("Password reset token queued successfully for email: {Email}", email);
            return Result<string>.Success(default!, "If the account exists, a password reset email has been sent.", 200);
        }

        public async Task<Result<string>> ResetPassword(string token, PasswordResetReqDto req)
        {
            _logger.LogInformation("Attempting to reset password using token.");
            await _passwordResetValidator.EnsureValidAsync(req);

            var decodedToken = token.Replace(" ", "+");
            var decodedTokenHash = _tokenGenerator.HashOpaqueToken(decodedToken);
            var storedToken = await _tokenRepository.GetPasswordResetTokenAsync(decodedTokenHash);
            if (storedToken is null)
            {
                // Transitional fallback: upgrade legacy plaintext token rows once they are consumed.
                storedToken = await _tokenRepository.GetPasswordResetTokenAsync(decodedToken);
                if (storedToken is not null)
                {
                    storedToken.SetTokenHash(decodedTokenHash);
                }
            }
            if (storedToken == null || storedToken.IsUsed || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired password reset token.");
                throw new UnauthorizedException("Invalid or expired password reset token");
            }

            var user = await _userRepository.GetUserEntityByIdAsync(storedToken.UserId)
                       ?? throw new NotFoundException("User not found");

            var samePassword = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.NewPassword);
            if (samePassword == PasswordVerificationResult.Success)
            {
                throw new Domain.Exceptions.ValidationException("New password cannot be the same as the old password");
            }

            var hashedPassword = _passwordHasher.HashPassword(user, req.NewPassword);
            user.SetPassword(hashedPassword);
            storedToken.MarkAsUsed();
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Password reset successfully for user {UserId}.", user.Id);
            return Result<string>.Success(default!, "Password reset successfully", 200);
        }

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex MyRegex();
    }
}