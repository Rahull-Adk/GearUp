using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Users
{
    public class KycService : IKycService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly ICloudinaryImageUploader _cloudinaryImageUploader;
        private readonly ILogger<KycService> _logger;
        public KycService(IUserRepository userRepo, IMapper mapper, IDocumentProcessor documentProcessor, ICloudinaryImageUploader cloudinaryImageUploader, ILogger<KycService> logger)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _documentProcessor = documentProcessor;
            _cloudinaryImageUploader = cloudinaryImageUploader;
            _logger = logger;
        }

        public async Task<Result<KycUserResponseDto>> SubmitKycService(string userId, KycRequestDto req)
        {
            _logger.LogInformation("Submitting KYC for user ID: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
                return Result<KycUserResponseDto>.Failure("Unauthorized", 401);

            // Use GetUserEntityByIdAsync to retrieve the full User entity (matches repository mock and intent)
            var user = await _userRepo.GetUserEntityByIdAsync(Guid.Parse(userId));
            if (user == null)
                return Result<KycUserResponseDto>.Failure("User not found", 404);

            if (req.DocumentType == KycDocumentType.Default)
                return Result<KycUserResponseDto>.Failure("Invalid document type.", 400);

            var processedDocuments = await _documentProcessor.ProcessDocuments(req.Kyc, 1200, 800);
            if (!processedDocuments.IsSuccess)
            {
                _logger.LogError("Document processing failed for user ID: {UserId} with error: {ErrorMessage}", userId, processedDocuments.ErrorMessage);

                return Result<KycUserResponseDto>.Failure(processedDocuments.ErrorMessage, processedDocuments.Status);
            }

            _logger.LogInformation("Document processing succeeded for user ID: {UserId}", userId);

            var docsPath = $"gearup/users/{user.Id}/kyc";
            var (imageStreams, pdfStreams) = processedDocuments.Data;

            var imageUrls = await _cloudinaryImageUploader.UploadImageListAsync(imageStreams, docsPath);

            var pdfUrls = await _cloudinaryImageUploader.UploadImageListAsync(pdfStreams, docsPath);
            if (imageUrls.Count == 0 && pdfUrls.Count == 0)
            {
                _logger.LogError("No Images were uploaded for user ID: {UserId}", userId);
                return Result<KycUserResponseDto>.Failure("Failed to upload KYC documents.", 500);
            }

            var documentUrls = imageUrls.Concat(pdfUrls).ToList();

            if (documentUrls.Count == 0)
            {
                _logger.LogError("No pdfs were uploaded for user ID: {UserId}", userId);   
                return Result<KycUserResponseDto>.Failure("Failed to upload KYC documents.", 500);
            }

           

            var selfiePath = $"gearup/users/{user.Id}/kyc/selfie";
            var processedSelfie = await _documentProcessor.ProcessImage(req.SelfieImage, 800, 800, true);
            if (!processedSelfie.IsSuccess)
                return Result<KycUserResponseDto>.Failure(processedSelfie.ErrorMessage, processedSelfie.Status);
            _logger.LogInformation("Selfie processing succeeded for user ID: {UserId}", userId);

            var selfieUrl = await _cloudinaryImageUploader.UploadImageListAsync(new List<MemoryStream> { processedSelfie.Data }, selfiePath);

            if (selfieUrl.Count == 0)
            {
                _logger.LogError("No selfie image was uploaded for user ID: {UserId}", userId);
                return Result<KycUserResponseDto>.Failure("Failed to upload selfie image.", 500);
            }

            _logger.LogInformation("KYC documents uploaded successfully for user ID: {UserId}", userId);
            var kycSubmission = KycSubmissions.CreateKycSubmissions(user.Id, req.DocumentType, documentUrls, selfieUrl.First().ToString());

            await _userRepo.AddKycAsync(kycSubmission);
            await _userRepo.SaveChangesAsync();
            _logger.LogInformation("KYC submission created successfully for user ID: {UserId}", userId);
            return Result<KycUserResponseDto>.Success(null!, "KYC submission successful.", 200);
        }
    }
}
