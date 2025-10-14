using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Services.Auth
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly IUserRepository _userRepository;
        public EmailVerificationService(ITokenValidator tokenValidator, IUserRepository userRepository)
        {
            _tokenValidator = tokenValidator;
            _userRepository = userRepository;
        }
        public async Task<Result<string>> VerifyEmail(string token)
        {
            string secretKey = Environment.GetEnvironmentVariable("EmailVerificationToken_SecretKey")!;
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
            User.VerifyEmail(user);
            await _userRepository.SaveChangesAsync();
            return Result<string>.Success(null, "Email verified successfully", 200);

        }
    }
}
