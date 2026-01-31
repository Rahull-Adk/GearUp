using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.Services.Admin;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GearUp.UnitTests.Application.Admin
{
    public class AdminTest
    {
        private readonly Mock<IAdminRepository> _mockAdminRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<ICarRepository> _mockCarRepository = new();
        private readonly Mock<ILogger<GeneralAdminService>> _mockLogger = new();
        private readonly Mock<IRealTimeNotifier> _mockNotifier = new();

        private GeneralAdminService CreateService() => new(
            _mockAdminRepository.Object,
            _mockUserRepository.Object,
            _mockCarRepository.Object,
            _mockLogger.Object,
            _mockNotifier.Object
        );


        [Fact]
        public async Task GetAllKyc_ShouldReturnResult_WhenCacheIsNull()
        {
            // Arrange
            var input = new ToAdminKycListResponseDto(
                new List<ToAdminKycResponseDto>
                {
                    new ToAdminKycResponseDto
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.NewGuid(),
                        Status = KycStatus.Pending,
                        SubmittedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new ToAdminKycResponseDto
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.NewGuid(),
                        Status = KycStatus.Approved,
                        SubmittedAt = DateTime.UtcNow.AddDays(-5)
                    }
                },
                2
            );

            _mockAdminRepository.Setup(a => a.GetAllKycSubmissionsAsync()).ReturnsAsync(input);

            //Act
            var svc = CreateService();
            var result = await svc.GetAllKycs();

            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.TotalCount);
            Assert.Equal("KYC submissions retrieved successfully", result.SuccessMessage);
        }

        [Fact]
        public async Task GetAllKyc_ShouldReturnNoKycResult_WhenNoKycSubmissionsExist()
        {
            // Arrange
            var empty = new ToAdminKycListResponseDto(new List<ToAdminKycResponseDto>(), 0);
            _mockAdminRepository.Setup(a => a.GetAllKycSubmissionsAsync()).ReturnsAsync(empty);

            // Act
            var svc = CreateService();
            var result = await svc.GetAllKycs();
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal("No KYC submissions yet", result.SuccessMessage);
        }

        [Fact]
        public async Task GetKycById_ShouldReturnResult_WhenCacheIsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ToAdminKycResponseDto
            {
                Id = id,
                UserId = Guid.NewGuid(),
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddDays(-2)
            };

            _mockAdminRepository.Setup(a => a.GetKycSubmissionByIdAsync(id)).ReturnsAsync(dto);

            //Act
            var svc = CreateService();
            var result = await svc.GetKycById(id);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal("KYC submission retrieved successfully", result.SuccessMessage);


        }

        [Fact]
        public async Task GetKycById_ShouldReturnNotFoundResult_WhenKycDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockAdminRepository.Setup(a => a.GetKycSubmissionByIdAsync(id)).ReturnsAsync((ToAdminKycResponseDto?)null);

            //Act
            var svc = CreateService();
            var result = await svc.GetKycById(id);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.Status);
            Assert.False(result.IsSuccess);
            Assert.Equal("KYC submission not found", result.ErrorMessage);
        }

        [Theory]
        [InlineData(KycStatus.Pending)]
        [InlineData(KycStatus.Approved)]
        [InlineData(KycStatus.Rejected)]
        public async Task GetKycByStatus_ShouldReturnResult_WhenCacheIsNull(KycStatus status)
        {
            // Arrange
            var list = new List<ToAdminKycResponseDto>
            {
                new ToAdminKycResponseDto { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = status, SubmittedAt = DateTime.UtcNow },
                new ToAdminKycResponseDto { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = status, SubmittedAt = DateTime.UtcNow }
            };
            var response = new ToAdminKycListResponseDto(list, list.Count);
            _mockAdminRepository.Setup(a => a.GetKycSubmissionsByStatusAsync(status)).ReturnsAsync(response);

            //Act
            var svc = CreateService();
            var result = await svc.GetKycsByStatus(status);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal(list.Count, result.Data.TotalCount);
            Assert.Equal("KYC submissions retrieved successfully", result.SuccessMessage);

        }

        [Theory]
        [InlineData(KycStatus.Pending)]
        [InlineData(KycStatus.Approved)]
        [InlineData(KycStatus.Rejected)]
        public async Task GetKycByStatus_ShouldReturnMessage_WhenKycIsNotPresent(KycStatus status)
        {
            // Arrange
            var empty = new ToAdminKycListResponseDto(new List<ToAdminKycResponseDto>(), 0);
            _mockAdminRepository.Setup(a => a.GetKycSubmissionsByStatusAsync(status)).ReturnsAsync(empty);
            //Act
            var svc = CreateService();
            var result = await svc.GetKycsByStatus(status);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal("No KYC submissions found with the specified status", result.SuccessMessage);

        }

        [Fact]
        public async Task GetKycByStatus_ShouldReturnMessage_WhenInvalidStatusIsProvided()
        {
            // Arrange
            var invalidStatus = (KycStatus)999; // Invalid enum value
            // Act
            var svc = CreateService();
            var result = await svc.GetKycsByStatus(invalidStatus);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.Status);
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid KYC status", result.ErrorMessage);
        }

        [Theory]
        [InlineData("123e4567-e89b-12d3-a456-426614174000", KycStatus.Rejected, "123e4567-e89b-12d3-a456-426614174123", "Invalid documents")]
        public async Task UpdateKycStatus_ShouldReturnSuccessResult_WhenKycIsUpdated(string kycId, KycStatus status, string reviewerId, string? rejectionReason)
        {
            // Arrange
            var guidKycId = Guid.Parse(kycId);
            var guidReviewerId = Guid.Parse(reviewerId);
            var mockKyc = KycSubmissions.CreateKycSubmissions(
              guidKycId,
                KycDocumentType.Passport,
                new List<Uri> { new Uri("http://example.com/document1.jpg") },
                "A1234567"
            );
            var mockUser = User.CreateLocalUser("user", "user@gmail.com", "proshane");

            _mockAdminRepository.Setup(a => a.GetKycEntityByIdAsync(guidKycId)).ReturnsAsync(mockKyc);
            _mockUserRepository.Setup(u => u.GetUserEntityByIdAsync(mockKyc.UserId)).ReturnsAsync(mockUser);

            // Act
            var svc = CreateService();
            var result = await svc.UpdateKycStatus(guidKycId, status, guidReviewerId, rejectionReason);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal("KYC status updated successfully", result.SuccessMessage);
        }

    }
}