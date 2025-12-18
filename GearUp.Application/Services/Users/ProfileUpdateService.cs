using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GearUp.Application.Services.Users
{
    public class ProfileUpdateService : IProfileUpdateService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly ICloudinaryImageUploader _cloudinaryImageUploader;
        private readonly ILogger<ProfileUpdateService> _logger;
        public ProfileUpdateService(IUserRepository userRepo, IMapper mapper, IPasswordHasher<User> passwordHasher, IEmailSender emailSender, ITokenGenerator tokenGenerator, IDocumentProcessor documentProcessor, ICloudinaryImageUploader cloudinaryImageUploader, ILogger<ProfileUpdateService> logger)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _documentProcessor = documentProcessor;
            _tokenGenerator = tokenGenerator;
            _cloudinaryImageUploader = cloudinaryImageUploader;
            _logger = logger;
        }
        public async Task<Result<UpdateUserResponseDto>> UpdateUserProfileService(string userId, UpdateUserRequestDto reqDto)
        {
            _logger.LogInformation("Updating profile for user ID: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
                return Result<UpdateUserResponseDto>.Failure("Unauthorized", 401);

            var user = await _userRepo.GetUserEntityByIdAsync(Guid.Parse(userId));
            if (user == null)
                return Result<UpdateUserResponseDto>.Failure("User not found", 404);

            var validationResult = ValidateProfileChanges(user, reqDto);
            if (validationResult is not null)
                return validationResult;

            var passwordResult = HandlePasswordUpdateAsync(user, reqDto);
            if (!passwordResult.IsSuccess)
                return Result<UpdateUserResponseDto>.Failure(passwordResult.ErrorMessage, passwordResult.Status);

            var newHashedPassword = passwordResult.Data;


            var avatarUrl = await HandleAvatarUpdateAsync(user, reqDto);

            var emailUpdateResult = await HandleEmailUpdateAsync(user, reqDto);
            if (emailUpdateResult is not null)
                return emailUpdateResult;
            _logger.LogInformation("Email update handled for user ID: {UserId}", userId);

            user.UpdateProfile(
                reqDto.Name,
                reqDto.PhoneNumber,
                avatarUrl,
                reqDto.DateOfBirth,
                newHashedPassword
            );

            await _userRepo.SaveChangesAsync();

            var message = string.IsNullOrEmpty(reqDto.NewEmail)
                ? "Profile updated successfully"
                : "Please verify your new email first.";

            _logger.LogInformation("Profile updated successfully for user ID: {UserId}", userId);
            return Result<UpdateUserResponseDto>.Success(null!, message, 200);
        }


        private static Result<UpdateUserResponseDto>? ValidateProfileChanges(User user, UpdateUserRequestDto reqDto)
        {
            if (reqDto.Name != null && string.Equals(user.Name, reqDto.Name, StringComparison.OrdinalIgnoreCase))
                return Result<UpdateUserResponseDto>.Failure("New name cannot be same with the old one.", 400);

            if (reqDto.PhoneNumber != null && string.Equals(user.PhoneNumber, reqDto.PhoneNumber, StringComparison.OrdinalIgnoreCase))
                return Result<UpdateUserResponseDto>.Failure("New phone number cannot be same with the old one.", 400);

            if (reqDto.DateOfBirth != null && reqDto.DateOfBirth == user.DateOfBirth)
                return Result<UpdateUserResponseDto>.Failure("New date of birth cannot be same with the old one.", 400);

            return null;
        }

        private Result<string?> HandlePasswordUpdateAsync(User user, UpdateUserRequestDto reqDto)
        {
            if (!PasswordUpdateAttempted(reqDto))
                return Result<string?>.Success(null, "No password change requested", 200);

            if (string.IsNullOrWhiteSpace(reqDto.CurrentPassword))
                return Result<string?>.Failure("Current password is required.", 400);

            if (string.IsNullOrWhiteSpace(reqDto.NewPassword))
                return Result<string?>.Failure("New password is required.", 400);

            if (string.IsNullOrWhiteSpace(reqDto.ConfirmedNewPassword))
                return Result<string?>.Failure("Confirmed new password is required.", 400);

            if (reqDto.NewPassword != reqDto.ConfirmedNewPassword)
                return Result<string?>.Failure("New password and confirmed password do not match.", 400);

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, reqDto.CurrentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                return Result<string?>.Failure("Invalid current password.", 400);

            if (reqDto.CurrentPassword == reqDto.NewPassword)
                return Result<string?>.Failure("New password cannot be the same as the current password.", 400);

            var newHashedPassword = _passwordHasher.HashPassword(user, reqDto.NewPassword);
            _logger.LogInformation("Password updated successfully for user ID: {UserId}", user.Id);
            return Result<string?>.Success(newHashedPassword, "Password updated successfully", 200);
        }


        private async Task<string> HandleAvatarUpdateAsync(User user, UpdateUserRequestDto reqDto)
        {
            var defaultAvatarUrl = "https://i.pravatar.cc/300";

            if (reqDto.AvatarImage is null)
                return user.AvatarUrl ?? defaultAvatarUrl;

            if (user.AvatarUrl != defaultAvatarUrl)
            {
                var publicId = _cloudinaryImageUploader.ExtractPublicId(user.AvatarUrl);
                await _cloudinaryImageUploader.DeleteImageAsync(publicId);
            }

            var processedImage = await _documentProcessor.ProcessImage(reqDto.AvatarImage, 256, 256, forcedSquare: true);
            if (!processedImage.IsSuccess)
                throw new Exception(processedImage.ErrorMessage);

            var uploadPath = $"gearup/users/{user.Id}/avatar";
            var imageUrl = await _cloudinaryImageUploader.UploadImageListAsync(new List<MemoryStream> { processedImage.Data }, uploadPath);
            _logger.LogInformation("Avatar updated successfully for user ID: {UserId}", user.Id);
            return "Avatar updated successfully";
        }

        private async Task<Result<UpdateUserResponseDto>?> HandleEmailUpdateAsync(User user, UpdateUserRequestDto reqDto)
        {
            if (string.IsNullOrEmpty(reqDto.NewEmail))
                return null;

            if (string.Equals(user.Email, reqDto.NewEmail, StringComparison.OrdinalIgnoreCase))
                return Result<UpdateUserResponseDto>.Failure("New email cannot be same with the old one.", 400);

            var existingUser = await _userRepo.GetUserEntityByEmailAsync(reqDto.NewEmail);
            if (existingUser != null)
                return Result<UpdateUserResponseDto>.Failure("Email is already in use", 400);

            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", reqDto.NewEmail),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("purpose", "email_reset_verification")
            };

            user.SetPendingEmail(reqDto.NewEmail);
            user.SetIsPendingEmailVerified(false);
            await _userRepo.SaveChangesAsync();

            var token = _tokenGenerator.GenerateEmailVerificationToken(claims);
            await _emailSender.SendEmailReset(reqDto.NewEmail, token);

            return Result<UpdateUserResponseDto>.Success(null!, "Please verify your new email first.", 200);
        }

        private static bool PasswordUpdateAttempted(UpdateUserRequestDto reqDto)
        {
            return reqDto.CurrentPassword != null ||
                   reqDto.NewPassword != null ||
                   reqDto.ConfirmedNewPassword != null;
        }

    }
}