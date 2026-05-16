using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Users;
using GearUp.Domain.Enums;
using GearUp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;


namespace GearUp.UnitTests.Application.Users
{
    public class GeneralUserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPostRepository> _postRepo = new();
        private readonly Mock<ILogger<GeneralUserService>> _logger = new();

        private GeneralUserService CreateService() => new(
            _userRepo.Object,
            _postRepo.Object,
            _logger.Object);

        [Fact]
        public async Task GetCurrentUserProfile_ShouldThrowNotFound_WhenUserNotFound()
        {
            var id = Guid.NewGuid();
            _userRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync((RegisterResponseDto?)null);
            var svc = CreateService();
            
            await Assert.ThrowsAsync<NotFoundException>(() => svc.GetCurrentUserProfileService(id.ToString()));
        }

        [Fact]
        public async Task GetUserProfile_ShouldThrowNotFound_WhenUserNotFound()
        {
            var username = "missing";
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((RegisterResponseDto?)null);
            var svc = CreateService();
            
            await Assert.ThrowsAsync<NotFoundException>(() => svc.GetUserProfile(username));
        }

        [Fact]
        public async Task GetCurrentUserProfile_ShouldThrowValidation_WhenUserIdIsEmpty()
        {
            var svc = CreateService();

            await Assert.ThrowsAsync<Domain.Exceptions.ValidationException>(() => svc.GetCurrentUserProfileService(string.Empty));
        }

        [Fact]
        public async Task GetCurrentUserProfile_ShouldThrowValidation_WhenUserIdIsMalformed()
        {
            var svc = CreateService();

            await Assert.ThrowsAsync<Domain.Exceptions.ValidationException>(() => svc.GetCurrentUserProfileService("not-a-guid"));
        }

        [Fact]
        public async Task GetCurrentUserProfile_ShouldReturnSuccess_WhenUserExists()
        {
            var id = Guid.NewGuid();
            var dto = new RegisterResponseDto(
                id,
                null,
                "john",
                "john@example.com",
                "John Doe",
                UserRole.Customer,
                new DateOnly(1990, 1, 1),
                null,
                string.Empty);
            _userRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(dto);
            var svc = CreateService();

            var res = await svc.GetCurrentUserProfileService(id.ToString());

            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            Assert.NotNull(res.Data);
            Assert.Equal("john", res.Data.Username);
        }

        [Fact]
        public async Task GetUserProfile_ShouldThrowValidation_WhenUsernameIsEmpty()
        {
            var svc = CreateService();

            await Assert.ThrowsAsync<Domain.Exceptions.ValidationException>(() => svc.GetUserProfile(string.Empty));
        }

        [Fact]
        public async Task GetUserProfile_ShouldThrowNotFound_WhenUserIsAdmin()
        {
            var username = "admin-user";
            var dto = new RegisterResponseDto(
                Guid.NewGuid(),
                null,
                username,
                "admin@example.com",
                "Admin User",
                UserRole.Admin,
                new DateOnly(1990, 1, 1),
                null,
                string.Empty);
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(dto);
            var svc = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() => svc.GetUserProfile(username));
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnSuccess_WhenNonAdminUserExists()
        {
            var username = "jane";
            var dto = new RegisterResponseDto(
                Guid.NewGuid(),
                null,
                username,
                "jane@example.com",
                "Jane Dealer",
                UserRole.Dealer,
                new DateOnly(1990, 1, 1),
                null,
                string.Empty);
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(dto);
            var svc = CreateService();

            var res = await svc.GetUserProfile(username);

            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            Assert.NotNull(res.Data);
            Assert.Equal(username, res.Data.Username);
        }
    }
}
