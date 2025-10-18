using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Domain.Entities.Users;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GearUp.Application.Services.Auth
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<Settings> _emailVerification_SecretKey;
        public EmailVerificationService(ITokenValidator tokenValidator, ITokenGenerator tokenGenerator, IUserRepository userRepository, IEmailSender emailSender, IOptions<Settings> emailVerification_SecretKey )
        {
            _tokenValidator = tokenValidator;
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _emailVerification_SecretKey = emailVerification_SecretKey;
        }

        public async Task<Result<string>> ResendVerificationEmail(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return Result<string>.Failure("User not found", 404);
            }
            if (user.IsEmailVerified == true)
            {
                return Result<string>.Failure("Email is already verified", 400);
            }
            var claims = new[]
                {
                   new Claim("id", user.Id.ToString()),
                   new Claim("email", user.Email),
                   new Claim("role", user.Role.ToString()),
                   new Claim("purpose", "email_verification")
                };
            var token = _tokenGenerator.GenerateEmailVerificationToken(claims);
            await _emailSender.SendVerificationEmail(email, token);
            return Result<string>.Success(null, "Email sent successfully", 200);
        }

        public async Task<Result<string>> VerifyEmail(string token)
        {
            string secretKey = _emailVerification_SecretKey.Value.EmailVerificationToken_SecretKey;
            if (string.IsNullOrEmpty(secretKey))
            {
                return Result<string>.Failure("Email verification secret key is not configured", 500);
            }

            var result = await _tokenValidator.ValidateToken(token, secretKey, "email_verification");
            if (!result.IsValid)
            {
                return Result<string>.Failure(result.Error!, result.Status);
            }

            var userId = result.ClaimsPrincipal?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<string>.Failure("User ID not found in token", 404);
            }
            var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
            
            if (user == null)
            {
                return Result<string>.Failure("User not found", 404);
            }
            if (user.IsEmailVerified == true)
            {
                return Result<string>.Failure("Email is already verified", 400);
            }
            user.VerifyEmail(user);
            await _userRepository.SaveChangesAsync();
            return Result<string>.Success(null, "Email verified successfully", 200);

        }
    }
}
