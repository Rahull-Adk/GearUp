using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using AutoMapper;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.Services.Admin;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Admin
{
    public class AdminTest
    {
        private readonly Mock<ICacheService> _mockCacheService = new();
        private readonly Mock<IAdminRepository> _mockAdminRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<GeneralAdminService>> _mockLogger = new();

        private GeneralAdminService CreateService() => new(
            _mockAdminRepository.Object,
            _mockMapper.Object,
            _mockUserRepository.Object,
            _mockCacheService.Object,
            _mockLogger.Object
        );


        [Fact]
        public async Task GetAllKycs_ShouldReturnCacheResult_WhenCacheIsNotNull()
        {
            // Arrange

            ToAdminKycListResponseDto data = new ToAdminKycListResponseDto(new List<ToAdminKycResponseDto>
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
                },

                new ToAdminKycResponseDto
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Status = KycStatus.Rejected,
                    SubmittedAt = DateTime.UtcNow.AddDays(-10)
                }
            }, 3);

            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>("kyc:all")).ReturnsAsync(data);

            var svc = CreateService();

            // Act
            var result = await svc.GetAllKycs();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Data.TotalCount);
            Assert.Equal("KYC submissions retrieved from cache", result.SuccessMessage);

        }

        [Fact]
        public async Task GetAllKyc_ShouldReturnResult_WhenCacheIsNull()
        {
            // Arrange
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>("kyc:all")).ReturnsAsync((ToAdminKycListResponseDto)null!);
            List<Uri> documentUrls = new List<Uri>
            {
                new Uri("http://example.com/document1.jpg"),
                new Uri("http://example.com/document2.jpg")
            };
            ICollection<KycSubmissions> input = new List<KycSubmissions>
            {
                KycSubmissions.CreateKycSubmissions(Guid.NewGuid(), KycDocumentType.NationalID, documentUrls, "12345"),
                KycSubmissions.CreateKycSubmissions(Guid.NewGuid(), KycDocumentType.Passport, documentUrls, "67890")
            };
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching all KYC submissions")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            _mockAdminRepository.Setup(a => a.GetAllKycSubmissionsAsync()).ReturnsAsync(input);
            _mockMapper.Setup(m => m.Map<List<ToAdminKycResponseDto>>(It.IsAny<ICollection<KycSubmissions>>()))
                .Returns(new List<ToAdminKycResponseDto>
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
                });

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
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>("kyc:all")).ReturnsAsync((ToAdminKycListResponseDto)null!);
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching all KYC submissions")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            _mockAdminRepository.Setup(a => a.GetAllKycSubmissionsAsync()).ReturnsAsync(new List<KycSubmissions>());
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
        public async Task GetKycById_ShouldReturnCacheResult_WhenCacheIsNotNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycResponseDto>($"kyc:{id}")).ReturnsAsync(new ToAdminKycResponseDto
            {
                Id = id,
                UserId = Guid.NewGuid(),
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddDays(-2)
            });

            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submission with id: {id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            //Act
            var svc = CreateService();
            var result = await svc.GetKycById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.Equal("KYC submission retrieved from cache", result.SuccessMessage);
        }

        [Fact]
        public async Task GetKycById_ShouldReturnResult_WhenCacheIsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycResponseDto>($"kyc:{id}")).ReturnsAsync((ToAdminKycResponseDto)null!);
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submission with id: {id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            _mockAdminRepository.Setup(a => a.GetKycSubmissionByIdAsync(id)).ReturnsAsync(KycSubmissions.CreateKycSubmissions(
                Guid.NewGuid(),
                KycDocumentType.Passport,
                new List<Uri> { new Uri("http://example.com/document1.jpg") },
                "A1234567"
            ));
            _mockMapper.Setup(m => m.Map<ToAdminKycResponseDto>(It.IsAny<KycSubmissions>())).Returns(new ToAdminKycResponseDto
            {
                Id = id,
                UserId = Guid.NewGuid(),
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddDays(-2)
            });

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
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycResponseDto>($"kyc:{id}")).ReturnsAsync((ToAdminKycResponseDto)null!);
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submission with id: {id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            _mockAdminRepository.Setup(a => a.GetKycSubmissionByIdAsync(id)).ReturnsAsync((KycSubmissions?)null);

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
        public async Task GetKycBuyStatus_ShouldReturnCacheResult_WhenCacheIsNotNull(KycStatus status)
        {
            // Arrange
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submissions with status: {status}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>($"kyc:status:{status}")).ReturnsAsync(new ToAdminKycListResponseDto(new List<ToAdminKycResponseDto>
            {
                new ToAdminKycResponseDto
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Status = status,
                    SubmittedAt = DateTime.UtcNow.AddDays(-2)
                }
            }, 1));

            //Act
            var svc = CreateService();
            var result = await svc.GetKycsByStatus(status);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data.TotalCount);
            Assert.Equal("KYC submissions retrieved from cache", result.SuccessMessage);
        }

        [Theory]
        [InlineData(KycStatus.Pending)]
        [InlineData(KycStatus.Approved)]
        [InlineData(KycStatus.Rejected)]
        public async Task GetKycByStatus_ShouldReturnResult_WhenCacheIsNull(KycStatus status)
        {
            // Arrange
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>($"kyc:status:{status}")).ReturnsAsync((ToAdminKycListResponseDto)null!);

            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submissions with status: {status}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            List<KycSubmissions> input = new List<KycSubmissions>
            {
                KycSubmissions.CreateKycSubmissions(Guid.NewGuid(), KycDocumentType.NationalID, new List<Uri>{ new Uri("http://example.com/document1.jpg") }, "12345", status),
                KycSubmissions.CreateKycSubmissions(Guid.NewGuid(), KycDocumentType.Passport, new List<Uri>{ new Uri("http://example.com/document2.jpg") }, "67890", status)
            };

            _mockAdminRepository.Setup(a => a.GetKycSubmissionsByStatusAsync(status)).ReturnsAsync(input);

            _mockMapper.Setup(m => m.Map<List<ToAdminKycResponseDto>>(It.IsAny<ICollection<KycSubmissions>>()))
                .Returns(input.Select(k => new ToAdminKycResponseDto
                {
                    Id = k.Id,
                    UserId = k.UserId,
                    Status = k.Status,
                    SubmittedAt = k.SubmittedAt
                }).ToList());

            //Act
            var svc = CreateService();
            var result = await svc.GetKycsByStatus(status);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Equal(input.Count, result.Data.TotalCount);
            Assert.Equal("KYC submissions retrieved successfully", result.SuccessMessage);

        }

        [Theory]
        [InlineData(KycStatus.Pending)]
        [InlineData(KycStatus.Approved)]
        [InlineData(KycStatus.Rejected)]
        public async Task GetKycByStatus_ShouldReturnMessage_WhenKycIsNotPresent(KycStatus status)
        {
            // Arrange
            _mockCacheService.Setup(c => c.GetAsync<ToAdminKycListResponseDto>($"kyc:status:{status}")).ReturnsAsync((ToAdminKycListResponseDto)null!);
            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching kyc submissions with status: {status}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            _mockAdminRepository.Setup(a => a.GetKycSubmissionsByStatusAsync(status)).ReturnsAsync(new List<KycSubmissions>());
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

            _mockLogger.Setup(l => l.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Updating KYC status for ID: {kycId} to {status}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            _mockAdminRepository.Setup(a => a.GetKycSubmissionByIdAsync(guidKycId)).ReturnsAsync(mockKyc);
            _mockUserRepository.Setup(u => u.GetUserByIdAsync(mockKyc.UserId)).ReturnsAsync(mockUser);
            _mockCacheService.Setup(c => c.RemoveAsync($"kyc:{kycId}")).Returns(Task.CompletedTask);

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