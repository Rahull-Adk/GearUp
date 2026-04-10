using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Users;
using GearUp.Domain.Enums;
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
        public async Task GetCurrentUserProfile_ShouldReturnError_WhenUserNotFound()
        {
            var id = Guid.NewGuid();
            _userRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync((RegisterResponseDto?)null);
            var svc = CreateService();
            var res = await svc.GetCurrentUserProfileService(id.ToString());
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnError_WhenUserNotFound()
        {
            var username = "missing";
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((RegisterResponseDto?)null);
            var svc = CreateService();
            var res = await svc.GetUserProfile(username);
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);
        }

        [Fact]
        public async Task GetCurrentUserProfile_ShouldReturn400_WhenUserIdIsEmpty()
        {
            var svc = CreateService();

            var res = await svc.GetCurrentUserProfileService(string.Empty);

            Assert.False(res.IsSuccess);
            Assert.Equal(400, res.Status);
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
        public async Task GetUserProfile_ShouldReturn400_WhenUsernameIsEmpty()
        {
            var svc = CreateService();

            var res = await svc.GetUserProfile(string.Empty);

            Assert.False(res.IsSuccess);
            Assert.Equal(400, res.Status);
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturn404_WhenUserIsAdmin()
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

            var res = await svc.GetUserProfile(username);

            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);
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
