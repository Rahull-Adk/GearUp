using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace GearUp.Application.Services.Auth
{
    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<PasswordResetReqDto> _passwordResetValidator;
        public LoginService(IUserRepository userRepository, ITokenRepository tokenRepository, IPasswordHasher<User> passwordHasher, ITokenGenerator tokenGenerator, IEmailSender emailSender, IValidator<LoginRequestDto> loginValidator, IValidator<PasswordResetReqDto> passwordResetValidator)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _emailSender = emailSender;
            _loginValidator = loginValidator;
            _passwordResetValidator = passwordResetValidator;
        }
        public async Task<Result<LoginResponseDto>> LoginUser(LoginRequestDto req)
        {
            var validationResult = await _loginValidator.ValidateAsync(req);
            
            if(validationResult.IsValid == false)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<LoginResponseDto>.Failure(errors, 400);
            }

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
            if (!user.IsEmailVerified)
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
            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
            await _userRepository.SaveChangesAsync();


            return Result<LoginResponseDto>.Success(new LoginResponseDto { AccessToken = accessToken, RefreshToken = refreshToken }, "User logged in successfully!", 200);
        }

        public async Task<Result<LoginResponseDto>> RotateRefreshToken(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
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
                   new Claim("role", user.Role.ToString()),
                   new Claim("purpose", "access_token")
            };
            var newAccessToken = _tokenGenerator.GenerateAccessToken(accessClaims);
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
            RefreshToken.CreateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7), user.Id);
            RefreshToken.Revoke(storedToken);
            await _userRepository.SaveChangesAsync();
            return Result<LoginResponseDto>.Success(new LoginResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken }, "Token Rotated Successfully", 200);
        }

        public async Task<Result<string>> SendPasswordResetToken(string email)
        {

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

            return Result<string>.Success(token, "Email sent successfully", 200);
        }

        public async Task<Result<string>> ResetPassword(string token, PasswordResetReqDto req)
        {
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
            return Result<string>.Success(null, "Password reset successfully", 200);
        }
    }
}
