using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.Services.Auth;
using GearUp.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GearUp.UnitTests.Application.Auth
{
    public class EmailVerificationServiceTests
    {
        private readonly Mock<ITokenValidator> _tokenValidator = new();
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<ILogger<EmailVerificationService>> _logger = new();
        private readonly IOptions<Settings> _options = Options.Create(new Settings { EmailVerificationToken_SecretKey = "secret" });

        private EmailVerificationService CreateService() => new(
        _tokenValidator.Object,
        _tokenGenerator.Object,
        _userRepo.Object,
        _emailSender.Object,
        _options,
        _logger.Object);

        [Fact]
        public async Task ResendVerification_Success_SendsEmail()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync(user.Email)).ReturnsAsync(user);
            _tokenGenerator.Setup(t => t.GenerateEmailVerificationToken(It.IsAny<IEnumerable<Claim>>())).Returns("token");
            var svc = CreateService();
            var result = await svc.ResendVerificationEmail(user.Email);
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            _emailSender.Verify(e => e.SendVerificationEmail(user.Email, "token"), Times.Once);
        }

        [Fact]
        public async Task ResendVerification_Fails_UserNotFound()
        {
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync("missing@example.com")).ReturnsAsync((User?)null);
            var svc = CreateService();
            var result = await svc.ResendVerificationEmail("missing@example.com");
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.Status);
        }

        [Fact]
        public async Task ResendVerification_Fails_AlreadyVerified()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John");
            user.VerifyEmail();
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync(user.Email)).ReturnsAsync(user);
            var svc = CreateService();
            var result = await svc.ResendVerificationEmail(user.Email);
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }

        [Fact]
        public async Task VerifyEmail_Success_EmailVerification()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John");
            var identity = new ClaimsIdentity(new[]
            {
 new Claim("id", user.Id.ToString()),
 new Claim("purpose", TokenPurposes.EmailVerification)
 });
            var principal = new ClaimsPrincipal(identity);
            _tokenValidator.Setup(v => v.ValidateToken("token", "secret", null))
            .ReturnsAsync(new TokenValidationResultModel { IsValid = true, ClaimsPrincipal = principal, Status = 200 });
            _userRepo.Setup(r => r.GetUserEntityByIdAsync(user.Id)).ReturnsAsync(user);
            var svc = CreateService();
            var result = await svc.VerifyEmail("token");
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.True(user.IsEmailVerified);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyEmail_Fails_InvalidToken()
        {
            _tokenValidator.Setup(v => v.ValidateToken("bad", "secret", null))
            .ReturnsAsync(new TokenValidationResultModel { IsValid = false, Error = "Invalid", Status = 401 });
            var svc = CreateService();
            var result = await svc.VerifyEmail("bad");
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.Status);
        }
    }
}