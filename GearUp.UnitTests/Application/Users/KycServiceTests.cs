using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.ServiceDtos.User;
using GearUp.Application.Services.Users;
using GearUp.Domain.Entities; 
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GearUp.UnitTests.Application.Users
{
    public class KycServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IDocumentProcessor> _docProcessor = new();
        private readonly Mock<ICloudinaryImageUploader> _uploader = new();
        private readonly Mock<ILogger<KycService>> _logger = new();

        private KycService CreateService() => new(
        _userRepo.Object,
        _mapper.Object,
        _docProcessor.Object,
        _uploader.Object,
        _logger.Object);

        [Fact]
        public async Task SubmitKyc_Success()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            var images = new List<MemoryStream> { new MemoryStream(new byte[] { 1 }) };
            var pdfs = new List<MemoryStream> { new MemoryStream(new byte[] { 2 }) };
            _docProcessor.Setup(d => d.ProcessDocuments(It.IsAny<List<IFormFile>>(), 1200, 800))
            .ReturnsAsync(Result<(List<MemoryStream>, List<MemoryStream>)>.Success((images, pdfs)));
            _docProcessor.Setup(d => d.ProcessImage(It.IsAny<IFormFile>(), 800, 800, true))
            .ReturnsAsync(Result<MemoryStream>.Success(new MemoryStream(new byte[] { 3 })));
            _uploader.Setup(u => u.UploadImageListAsync(It.IsAny<List<MemoryStream>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Uri> { new Uri("https://example.com/doc1"), new Uri("https://example.com/doc2") });
            _uploader.Setup(u => u.UploadImageListAsync(It.Is<List<MemoryStream>>(l => l.Count == 1), It.Is<string>(p => p.Contains("selfie"))))
            .ReturnsAsync(new List<Uri> { new Uri("https://example.com/selfie") });
            _mapper.Setup(m => m.Map<KycUserResponseDto>(It.IsAny<object>())).Returns(new KycUserResponseDto
            {
                Id = Guid.NewGuid(),
                SubmittedBy = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email, Role = "Customer", AvatarUrl = user.AvatarUrl },
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                DocumentUrls = new List<string> { "https://example.com/doc1", "https://example.com/doc2" },
                SelfieUrl = "https://example.com/selfie"
            });
            var svc = CreateService();
            var req = new KycRequestDto(KycDocumentType.Passport, new List<IFormFile>(), new FormFile(Stream.Null, 0, 0, "", ""));
            var res = await svc.SubmitKycService(user.Id.ToString(), req);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _userRepo.Verify(r => r.AddKycAsync(It.Is<KycSubmissions>(k => k.UserId == user.Id)), Times.Once);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task SubmitKyc_Fails_UserNotFound()
        {
            var svc = CreateService();
            var req = new KycRequestDto(KycDocumentType.Passport, new List<IFormFile>(), new FormFile(Stream.Null, 0, 0, "", ""));
            var res = await svc.SubmitKycService(Guid.NewGuid().ToString(), req);
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);
        }


        [Fact]
        public async Task SubmitKyc_Fails_DocumentProcessingError()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _docProcessor.Setup(d => d.ProcessDocuments(It.IsAny<List<IFormFile>>(), 1200, 800))
                .ReturnsAsync(Result<(List<MemoryStream>, List<MemoryStream>)>.Failure("Document processing error"));
            var svc = CreateService();
            var res = await svc.SubmitKycService(user.Id.ToString(), new KycRequestDto(KycDocumentType.Passport, new List<IFormFile>(), new FormFile(Stream.Null, 0, 0, "", "")));
            Assert.False(res.IsSuccess);
            Assert.Equal(500, res.Status);
        }

    }
}
