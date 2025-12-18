using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GearUp.Application.Services.Auth
{
    public sealed class EmailVerificationService : IEmailVerificationService
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<Settings> _emailVerification_SecretKey;
        private readonly ILogger<EmailVerificationService> _logger;
        public EmailVerificationService(ITokenValidator tokenValidator, ITokenGenerator tokenGenerator, IUserRepository userRepository, IEmailSender emailSender, IOptions<Settings> emailVerification_SecretKey, ILogger<EmailVerificationService> logger)
        {
            _tokenValidator = tokenValidator;
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _emailVerification_SecretKey = emailVerification_SecretKey;
            _logger = logger;
        }

        public async Task<Result<string>> ResendVerificationEmail(string email)
        {
            _logger.LogInformation("Resend verification email requested for {Email}", email);
            var user = await _userRepository.GetUserEntityByEmailAsync(email);
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
                   new Claim(ClaimTypes.Role, user.Role.ToString()),
                   new Claim("purpose", "email_verification")
                };
            var token = _tokenGenerator.GenerateEmailVerificationToken(claims);
            await _emailSender.SendVerificationEmail(email, token);
            _logger.LogInformation("Verification email sent to {Email}", email);
            return Result<string>.Success(null, "Email sent successfully", 200);
        }

        public async Task<Result<string>> VerifyEmail(string token)
        {
            _logger.LogInformation("Email verification attempt with token");
            var secretKey = _emailVerification_SecretKey.Value.EmailVerificationToken_SecretKey;
            if (string.IsNullOrEmpty(secretKey))
                return Result<string>.Failure("Email verification secret key is not configured", 500);

            var validation = await _tokenValidator.ValidateToken(token, secretKey);
            if (!validation.IsValid)
                return Result<string>.Failure(validation.Error!, validation.Status);

            var userId = validation.ClaimsPrincipal?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Result<string>.Failure("User ID not found in token", 404);

            var user = await _userRepository.GetUserEntityByIdAsync(Guid.Parse(userId));
            if (user == null)
                return Result<string>.Failure("User not found", 404);

            var purpose = validation.ClaimsPrincipal?.FindFirst("purpose")?.Value;
            if (string.IsNullOrEmpty(purpose))
            {
                _logger.LogWarning("Token purpose is missing for user ID: {UserId}", userId);
                return Result<string>.Failure("Token purpose is missing", 400);
            }

            switch (purpose)
            {
                case TokenPurposes.EmailVerification:
                    if (user.IsEmailVerified)
                        return Result<string>.Failure("Email already verified", 400);
                    user.VerifyEmail();
                    break;

                case TokenPurposes.EmailResetVerification:
                    if (user.IsPendingEmailVerified)
                        return Result<string>.Failure("New email already verified", 400);
                    user.VerifyPendingEmail();
                    break;

                default:
                    return Result<string>.Failure("Invalid verification purpose", 400);
            }

            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Email verified successfully for user ID: {UserId}", userId);
            return Result<string>.Success(null, "Email verified successfully", 200);
        }

    }
}
