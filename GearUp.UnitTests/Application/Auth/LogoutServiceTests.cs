using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Services.Auth;
using GearUp.Domain.Entities.Tokens;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Auth
{
    public class LogoutServiceTests
    {
        private readonly Mock<ITokenRepository> _tokenRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<ILogger<LogoutService>> _logger = new();

        [Fact]
        public async Task Logout_Success_RevokesToken()
        {
            var token = RefreshToken.CreateRefreshToken("r", DateTime.UtcNow.AddMinutes(10), Guid.NewGuid());
            _tokenRepo.Setup(r => r.GetRefreshTokenAsync("r")).ReturnsAsync(token);
            var svc = new LogoutService(_tokenRepo.Object, _userRepo.Object, _logger.Object);
            var result = await svc.Logout("r");
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Logout_Fails_WhenTokenMissing()
        {
            _tokenRepo.Setup(r => r.GetRefreshTokenAsync("missing")).ReturnsAsync((RefreshToken?)null);
            var svc = new LogoutService(_tokenRepo.Object, _userRepo.Object, _logger.Object);
            var result = await svc.Logout("missing");
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }
    }
}
