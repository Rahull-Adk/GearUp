using AutoMapper;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.User;
using GearUp.Application.Services.Users;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GearUp.UnitTests.Application.Users
{
    public class ProfileUpdateServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<IDocumentProcessor> _docProcessor = new();
        private readonly Mock<ICloudinaryImageUploader> _uploader = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ILogger<ProfileUpdateService>> _logger = new();

        private ProfileUpdateService CreateService() => new(
        _userRepo.Object,
        _mapper.Object,
        _passwordHasher.Object,
        _emailSender.Object,
        _tokenGenerator.Object,
        _docProcessor.Object,
        _uploader.Object,
        _logger.Object);

        [Fact]
        public async Task UpdateProfile_Fails_UserNotFound()
        {
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(Guid.NewGuid().ToString(), new UpdateUserRequestDto(null, null, null, null, null, null, null, null));
            Assert.False(res.IsSuccess);
        }

        [Fact]
        public async Task UpdateProfile_UpdatesBasicFields()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, It.IsAny<string>())).Returns(PasswordVerificationResult.Failed);
            var dto = new UpdateUserRequestDto(null, "Jane", null, null, "123", null, null, null);
            _mapper.Setup(m => m.Map<UpdateUserResponseDto>(user)).Returns(new UpdateUserResponseDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            PendingEmail: user.PendingEmail,
            Name: user.Name,
            AvatarUrl: user.AvatarUrl,
            IsEmailVerified: user.IsEmailVerified,
            IsPendingEmailVerified: user.IsPendingEmailVerified,
            UpdatedAt: DateTime.UtcNow));
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_EmailChange_SendsVerification()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync("new@example.com")).ReturnsAsync((User?)null);
            _tokenGenerator.Setup(t => t.GenerateEmailVerificationToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("token");
            var dto = new UpdateUserRequestDto("new@example.com", null, null, null, null, null, null, null);
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _emailSender.Verify(e => e.SendEmailReset("new@example.com", "token"), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_EmailChange_Fails_EmailInUse()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync("new@example.com")).ReturnsAsync(user);
            var dto = new UpdateUserRequestDto("new@example.com", null, null, null, null, null, null, null);
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.False(res.IsSuccess);
            Assert.Equal(400, res.Status);
        }

        [Fact]
        public async Task UpdateProfile_PasswordChange_Succeeds()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, It.IsAny<string>())).Returns(PasswordVerificationResult.Success);
            var dto = new UpdateUserRequestDto(null, null, null, null, null, "currPassword", "newPassword", "newPassword");
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_PasswordChange_Fails_WrongCurrentPassword()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, It.IsAny<string>())).Returns(PasswordVerificationResult.Failed);
            var dto = new UpdateUserRequestDto(null, null, null, null, null, "currPassword", "newPassword", "newPassword");
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.False(res.IsSuccess);
            Assert.Equal(400, res.Status);
        }

        [Fact]
        public async Task UpdateProfile_PasswordChange_Fails_MismatchNewPasswords()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            var dto = new UpdateUserRequestDto(null, null, null, null, null, "currPassword", "newPassword1", "newPassword2");
            var svc = CreateService();
            var res = await svc.UpdateUserProfileService(user.Id.ToString(), dto);
            Assert.False(res.IsSuccess);
            Assert.Equal(400, res.Status);
        }

    }
}
