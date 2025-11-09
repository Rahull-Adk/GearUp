using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace GearUp.Application.Services.Auth
{
    public sealed class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<AdminLoginRequestDto> _adminLoginValidator;
        private readonly IValidator<PasswordResetReqDto> _passwordResetValidator;
        private readonly ILogger<LoginService> _logger;
        public LoginService(IUserRepository userRepository, ITokenRepository tokenRepository, IPasswordHasher<User> passwordHasher, ITokenGenerator tokenGenerator, IEmailSender emailSender, IValidator<LoginRequestDto> loginValidator, IValidator<AdminLoginRequestDto> adminLoginValidator ,  IValidator<PasswordResetReqDto> passwordResetValidator, ILogger<LoginService> logger)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _emailSender = emailSender;
            _loginValidator = loginValidator;
            _adminLoginValidator = adminLoginValidator;
            _passwordResetValidator = passwordResetValidator;
            _logger = logger;
        }
        public async Task<Result<LoginResponseDto>> LoginUser(LoginRequestDto req)
        {
            _logger.LogInformation("Attempting to log in user with identifier: {Identifier}", req.UsernameOrEmail);
            var validationResult = await _loginValidator.ValidateAsync(req);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<LoginResponseDto>.Failure(errors, 400);
            }

            Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            var user = emailRegex.IsMatch(req.UsernameOrEmail)
                ? await _userRepository.GetUserByEmailAsync(req.UsernameOrEmail)
                : await _userRepository.GetUserByUsernameAsync(req.UsernameOrEmail);

            return await HandleLogin(user!, req.Password);
        }

        public async Task<Result<LoginResponseDto>> LoginAdmin(AdminLoginRequestDto req)
        {
            _logger.LogInformation("Attempting to log in admin with email: {Email}", req.Email);
            var validationResult = await _adminLoginValidator.ValidateAsync(req);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<LoginResponseDto>.Failure(errors, 400);
            }

            var user = await _userRepository.GetUserByEmailAsync(req.Email);
            if (user?.Role != UserRole.Admin)
                return Result<LoginResponseDto>.Failure("Admin not found.", 404);

            return await HandleLogin(user, req.Password);
        }



        private async Task<Result<LoginResponseDto>> HandleLogin(User user, string password)
        {
            if (user == null)
                return Result<LoginResponseDto>.Failure("User not found", 404);

            var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (passwordVerification == PasswordVerificationResult.Failed)
                return Result<LoginResponseDto>.Failure("Invalid Credentials", 401);

            if (user.Role != UserRole.Admin && !user.IsEmailVerified)
                return Result<LoginResponseDto>.Failure("Email not verified. Please verify your email to login.", 403);

            var accessClaims = new[]
            {
        new Claim("id", user.Id.ToString()),
        new Claim("email", user.Email),
        new Claim("role", user.Role.ToString()),
        new Claim("purpose", "access_token")
    };

            var accessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var refreshTokenEntity = RefreshToken.CreateRefreshToken(refreshToken, expiresAt, user.Id);
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
                return Result<LoginResponseDto>.Failure("Refresh token is missing", 401);
            }
            var storedToken = await _tokenRepository.GetRefreshTokenAsync(refreshToken);
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
                   new Claim(ClaimTypes.Role, user.Role.ToString()),
                   new Claim("purpose", "access_token")
            };
            var newAccessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
            var refreshTokenEntity = RefreshToken.CreateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7), user.Id);
            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            storedToken.Revoke();
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", user.Id);
            return Result<LoginResponseDto>.Success(new LoginResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken }, "Token Rotated Successfully", 200);
        }

        public async Task<Result<string>> SendPasswordResetToken(string email)
        {
            _logger.LogInformation("Attempting to send password reset token to email: {Email}", email);
            Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (emailRegex.IsMatch(email) == false)
            {
                return Result<string>.Failure("Invalid email format", 400);
            }

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return Result<string>.Failure("User not found", 404);
            }

            var token = _tokenGenerator.GeneratePasswordResetToken();

            var passwordRefreshTokenEntity = PasswordResetToken.CreatePasswordResetToken(token, DateTime.UtcNow.AddHours(1), user.Id);
            await _tokenRepository.AddPasswordResetTokenAsync(passwordRefreshTokenEntity);
            await _userRepository.SaveChangesAsync();

            await _emailSender.SendPasswordResetEmail(email, token);
            _logger.LogInformation("Password reset token sent successfully to email: {Email}", email);
            return Result<string>.Success(token, "Email sent successfully", 200);
        }

        public async Task<Result<string>> ResetPassword(string token, PasswordResetReqDto req)
        {
            _logger.LogInformation("Attempting to reset password using token.");
            var validationResult = await _passwordResetValidator.ValidateAsync(req);

            if (validationResult.IsValid == false)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<string>.Failure(errors, 400);
            }

            var decodedToken = token.Replace(" ", "+");
            var storedToken = await _tokenRepository.GetPasswordResetTokenAsync(decodedToken);
            if (storedToken == null || storedToken.IsUsed || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired password reset token.");
                return Result<string>.Failure("Invalid or expired password reset token", 401);
            }
            var user = await _userRepository.GetUserByIdAsync(storedToken.UserId);
            
            if (user == null)
            {
                return Result<string>.Failure("User not found", 404);
            }
            var samePassword = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.NewPassword);
            if(samePassword == PasswordVerificationResult.Success)
            {
                return Result<string>.Failure("New password cannot be the same as the old password", 400);
            }
            var hashedPassword = _passwordHasher.HashPassword(user, req.NewPassword);
            user.SetPassword(hashedPassword);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Password reset successfully for user {UserId}.", user.Id);
            return Result<string>.Success(null, "Password reset successfully", 200);
        }
    }
}
