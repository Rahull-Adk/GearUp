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
            _logger.Object);

        [Fact]
        public async Task GetCurrentUserProfile_ShouldReturnError_WhenUserNotFound()
        {
            var id = Guid.NewGuid();
            _cache.Setup(c => c.GetAsync<RegisterResponseDto>($"user:profile:{id}")).ReturnsAsync((RegisterResponseDto?)null);
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(id)).ReturnsAsync((User?)null);
            var svc = CreateService();
            var res = await svc.GetCurrentUserProfileService(id.ToString());
            Assert.False(res.IsSuccess);
            Assert.Equal(404, res.Status);

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
