using AutoMapper;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Users;
using GearUp.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Moq;


namespace GearUp.UnitTests.Application.Users
{
    public class GeneralUserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ILogger<GeneralUserService>> _logger = new();

        private GeneralUserService CreateService() => new(
            _userRepo.Object,
            _mapper.Object,
            _cache.Object,
            _logger.Object);

        [Fact]
        public async Task GetCurrentUserProfile_ReturnsFromCache()
        {
            var id = Guid.NewGuid();
            var dto = new RegisterResponseDto(id, null, "u", "e", "n", "r", "a");
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{id}")).ReturnsAsync(dto);
            var svc = CreateService();
            var res = await svc.GetCurrentUserProfileService(id.ToString());
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            Assert.Equal(dto, res.Data);
        }

        [Fact]
        public async Task GetCurrentUserProfile_FetchesAndCaches()
        {
            var id = Guid.NewGuid();
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{id}")).ReturnsAsync((RegisterResponseDto?)null);
            var user = User.CreateLocalUser("u", "e@example.com", "n");
            _userRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(user);
            var dto = new RegisterResponseDto(id, null, user.Username, user.Email, user.Name, "Customer", user.AvatarUrl);
            _mapper.Setup(m => m.Map<RegisterResponseDto>(user)).Returns(dto);
            var svc = CreateService();
            var res = await svc.GetCurrentUserProfileService(id.ToString());
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _cache.Verify(c => c.SetAsync($"user:profile:{id}", dto, null), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserProfile_ShouldReturnError_WhenUserNotFound()
        {
            var id = Guid.NewGuid();
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{id}")).ReturnsAsync((RegisterResponseDto?)null);
            _userRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync((User?)null);
            var svc = CreateService();
            var res = await svc.GetCurrentUserProfileService(id.ToString());
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);

        }

        [Fact]
        public async Task GetUserProfile_ReturnsFromCache()
        {
            var username = "john";
            var dto = new RegisterResponseDto(Guid.NewGuid(), null, username, "e", "n", "r", "a");
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{username}")).ReturnsAsync(dto);
            var svc = CreateService();
            var res = await svc.GetUserProfile(username);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            Assert.Equal(dto, res.Data);
        }

        [Fact]
        public async Task GetUserProfile_FetchesAndCaches()
        {
            var username = "john";
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{username}")).ReturnsAsync((RegisterResponseDto?)null);
            var user = User.CreateLocalUser(username, "e@example.com", "n");
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(user);
            var dto = new RegisterResponseDto(Guid.NewGuid(), null, user.Username, user.Email, user.Name, "Customer", user.AvatarUrl);
            _mapper.Setup(m => m.Map<RegisterResponseDto>(user)).Returns(dto);
            var svc = CreateService();
            var res = await svc.GetUserProfile(username);
            Assert.True(res.IsSuccess);
            Assert.Equal(200, res.Status);
            _cache.Verify(c => c.SetAsync($"user:profile:{username}", dto, null), Times.Once);
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnError_WhenUserNotFound()
        {
            var username = "missing";
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{username}")).ReturnsAsync((RegisterResponseDto?)null);
            _userRepo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((User?)null);
            var svc = CreateService();
            var res = await svc.GetUserProfile(username);
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);
        }
    }
}
