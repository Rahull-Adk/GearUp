using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace GearUp.Application.Services.Users
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly ICloudinaryImageUploader _cloudinaryImageUploader;
        public UserService(IUserRepository userRepo, IMapper mapper, IPasswordHasher<User> passwordHasher, IEmailSender emailSender, ITokenGenerator tokenGenerator, IDocumentProcessor documentProcessor, ICloudinaryImageUploader cloudinaryImageUploader)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _documentProcessor = documentProcessor;
            _tokenGenerator = tokenGenerator;
            _cloudinaryImageUploader = cloudinaryImageUploader;
        }
        public async Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RegisterResponseDto>.Failure("User ID cannot be empty", 400);
            }

            var guidId = Guid.Parse(userId);

            var user = await _userRepo.GetUserByIdAsync(guidId);
            if (user == null)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);

            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);
        }

        public async Task<Result<RegisterResponseDto>> GetUserProfile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return Result<RegisterResponseDto>.Failure("Username cannot be empty", 400);
            }

            var user = await _userRepo.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);

            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);
        }

        public async Task<Result<UpdateUserResponseDto>> UpdateUserProfileService(string userId, UpdateUserRequestDto reqDto)
        {
            if (string.IsNullOrEmpty(userId))
                return Result<UpdateUserResponseDto>.Failure("Unauthorized", 401);

            var user = await _userRepo.GetUserByIdAsync(Guid.Parse(userId));
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

            user.UpdateProfile(
                reqDto.Name,
                reqDto.PhoneNumber,
                avatarUrl,
                reqDto.DateOfBirth,
                newHashedPassword
            );

            await _userRepo.SaveChangesAsync();

            var mappedUser = _mapper.Map<UpdateUserResponseDto>(user);
            var message = string.IsNullOrEmpty(reqDto.NewEmail)
                ? "Profile updated successfully"
                : "Please verify your new email first.";

            return Result<UpdateUserResponseDto>.Success(mappedUser, message, 200);
        }


        public async Task<Result<KycResponseDto>> KycService(string userId, KycRequestDto req)
        {

            if (string.IsNullOrEmpty(userId))
                return Result<KycResponseDto>.Failure("Unauthorized", 401);

            var user = await _userRepo.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
                return Result<KycResponseDto>.Failure("User not found", 404);

            if (req.DocumentType == KycDocumentType.Default)
                return Result<KycResponseDto>.Failure("Invalid document type.", 400);

           var processedDocuments = await _documentProcessor.ProcessDocuments(req.Kyc, 1200, 800);
            if(!processedDocuments.IsSuccess)
                return Result<KycResponseDto>.Failure(processedDocuments.ErrorMessage, processedDocuments.Status);

            var docsPath = $"gearup/users/{user.Id}/kyc";
            var (imageStreams, pdfStreams) = processedDocuments.Data;

            var imageUrls = await _cloudinaryImageUploader.UploadImageListAsync(imageStreams, docsPath);

            var pdfUrls = await _cloudinaryImageUploader.UploadImageListAsync(pdfStreams, docsPath);
            if (imageUrls.Count == 0 && pdfUrls.Count == 0)
                return Result<KycResponseDto>.Failure("Failed to upload KYC documents.", 500);

            var documentUrls = imageUrls.Concat(pdfUrls).ToList();

            if (documentUrls.Count == 0)
                return Result<KycResponseDto>.Failure("Failed to upload KYC documents.", 500);

            var selfiePath = $"gearup/users/{user.Id}/kyc/selfie";
            var processedSelfie = await _documentProcessor.ProcessImage(req.SelfieImage, 800, 800, true);
            if (!processedSelfie.IsSuccess)
                return Result<KycResponseDto>.Failure(processedSelfie.ErrorMessage, processedSelfie.Status);

            var selfieUrl = await _cloudinaryImageUploader.UploadImageListAsync(new List<MemoryStream> { processedSelfie.Data }, selfiePath);
            if(selfieUrl.Count == 0)
                return Result<KycResponseDto>.Failure("Failed to upload selfie image.", 500);


            var kycSubmission = KycSubmissions.CreateKycSubmissions(user.Id, req.DocumentType, documentUrls, selfieUrl.First().ToString());

            await _userRepo.AddKycAsync(kycSubmission);
            await _userRepo.SaveChangesAsync();

           var responseData =  _mapper.Map<KycResponseDto>(kycSubmission);

            return Result<KycResponseDto>.Success(responseData, "KYC submission successful.", 200);
        }


        private static bool PasswordUpdateAttempted(UpdateUserRequestDto reqDto)
        {
            return reqDto.CurrentPassword != null ||
                   reqDto.NewPassword != null ||
                   reqDto.ConfirmedNewPassword != null;
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

            return imageUrl?.ToString() ?? defaultAvatarUrl;
        }

        private async Task<Result<UpdateUserResponseDto>?> HandleEmailUpdateAsync(User user, UpdateUserRequestDto reqDto)
        {
            if (string.IsNullOrEmpty(reqDto.NewEmail))
                return null;

            if (string.Equals(user.Email, reqDto.NewEmail, StringComparison.OrdinalIgnoreCase))
                return Result<UpdateUserResponseDto>.Failure("New email cannot be same with the old one.", 400);

            var existingUser = await _userRepo.GetUserByEmailAsync(reqDto.NewEmail);
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

    }
}
