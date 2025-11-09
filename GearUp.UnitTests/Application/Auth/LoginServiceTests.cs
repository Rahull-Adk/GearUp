using FluentValidation;
using FluentValidation.Results;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Domain.Entities.Tokens;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace GearUp.UnitTests.Application.Auth
{
    public class LoginServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<ITokenRepository> _tokenRepo = new();
        private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<IValidator<LoginRequestDto>> _loginValidator = new();
        private readonly Mock<IValidator<AdminLoginRequestDto>> _adminValidator = new();
        private readonly Mock<IValidator<PasswordResetReqDto>> _resetValidator = new();
        private readonly Mock<ILogger<LoginService>> _logger = new();

        private LoginService CreateService() => new(
            _userRepo.Object,
            _tokenRepo.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            _emailSender.Object,
            _loginValidator.Object,
            _adminValidator.Object,
            _resetValidator.Object,
            _logger.Object
        );

        private static ValidationResult Valid() => new ValidationResult();

        [Fact]
        public async Task LoginUser_Success_ReturnsTokens()
        {
            // Arrange
            var req = new LoginRequestDto { UsernameOrEmail = "john", Password = "secret" };
            _loginValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            user.VerifyEmail();
            _userRepo.Setup(r => r.GetUserByUsernameAsync("john")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "secret"))
            .Returns(PasswordVerificationResult.Success);
            _tokenGenerator.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>())).Returns("access");
            _tokenGenerator.Setup(t => t.GenerateRefreshToken()).Returns("refresh");

            var svc = CreateService();

            // Act
            var result = await svc.LoginUser(req);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.Equal("access", result.Data.AccessToken);
            Assert.Equal("refresh", result.Data.RefreshToken);
            _tokenRepo.Verify(tr => tr.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_Fails_WhenValidatorInvalid()
        {
            var req = new LoginRequestDto();
            _loginValidator.Setup(v => v.ValidateAsync(req, default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("UsernameOrEmail", "Required") }));

            var svc = CreateService();
            var result = await svc.LoginUser(req);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }

        [Fact]
        public async Task LoginUser_Fails_WhenEmailNotVerified()
        {
            var req = new LoginRequestDto { UsernameOrEmail = "john", Password = "secret" };
            _loginValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            _userRepo.Setup(r => r.GetUserByUsernameAsync("john")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "secret"))
         .Returns(PasswordVerificationResult.Success);
            var svc = CreateService();
            var result = await svc.LoginUser(req);

            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.Status);
        }

        [Fact]
        public async Task LoginFail_WhenPasswordIncorrect()
        {
            var req = new LoginRequestDto { UsernameOrEmail = "john", Password = "wrong" };
            _loginValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            user.VerifyEmail();
            _userRepo.Setup(r => r.GetUserByUsernameAsync("john")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
                .Returns(PasswordVerificationResult.Failed);
            var svc = CreateService();
            var result = await svc.LoginUser(req);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.Status);

        }

        [Fact]
        public async Task LoginAdmin_Fails_WhenNotAdmin()
        {
            var req = new AdminLoginRequestDto { Email = "user@example.com", Password = "p" };
            _adminValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            var user = User.CreateLocalUser("john", req.Email, "John");
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(user);

            var svc = CreateService();
            var result = await svc.LoginAdmin(req);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.Status);
        }

        [Fact]
        public async Task LoginAdmin_Success_ReturnsTokens()
        {
            var req = new AdminLoginRequestDto { Email = "admin@example.com", Password = "p" };
            _adminValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());

            var user = User.CreateLocalUser("admin", req.Email, "Admin User");
            user.VerifyEmail();
            user.SetRole(Domain.Enums.UserRole.Admin);
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, req.Password))
                .Returns(PasswordVerificationResult.Success);
            _tokenGenerator.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>())).Returns("admin-access");
            _tokenGenerator.Setup(t => t.GenerateRefreshToken()).Returns("admin-refresh");
            var svc = CreateService();
            var result = await svc.LoginAdmin(req);
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
        }

        [Fact]
        public async Task LoginAdmin_Fails_WhenPasswordIncorrect()
        {
            var req = new AdminLoginRequestDto { Email = "admin@example.com", Password = "wrong" };
            _adminValidator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            var user = User.CreateLocalUser("admin", req.Email, "Admin User");
            user.VerifyEmail();
            user.SetRole(Domain.Enums.UserRole.Admin);
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, req.Password))
                .Returns(PasswordVerificationResult.Failed);
            var svc = CreateService();
            var result = await svc.LoginAdmin(req);
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.Status);

        }

        [Fact]
        public async Task RotateRefreshToken_Success_IssuesNewTokens()
        {
            // Arrange
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            user.VerifyEmail();
            var stored = RefreshToken.CreateRefreshToken("old", DateTime.UtcNow.AddMinutes(10), user.Id);
            _tokenRepo.Setup(r => r.GetRefreshTokenAsync("old")).ReturnsAsync(stored);
            _userRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _tokenGenerator.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>())).Returns("new-access");
            _tokenGenerator.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

            var svc = CreateService();
            var result = await svc.RotateRefreshToken("old");

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.Equal("new-access", result.Data.AccessToken);
            Assert.Equal("new-refresh", result.Data.RefreshToken);
            _tokenRepo.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_Success_ChangesPassword()
        {
            var user = User.CreateLocalUser("john", "john@example.com", "John Doe");
            var token = PasswordResetToken.CreatePasswordResetToken("t", DateTime.UtcNow.AddMinutes(30), user.Id);
            _resetValidator.Setup(v => v.ValidateAsync(It.IsAny<PasswordResetReqDto>(), default)).ReturnsAsync(Valid());
            _tokenRepo.Setup(r => r.GetPasswordResetTokenAsync("t")).ReturnsAsync(token);
            _userRepo.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Failed);
            _passwordHasher.Setup(h => h.HashPassword(user, It.IsAny<string>())).Returns("hashed");

            var svc = CreateService();
            var result = await svc.ResetPassword("t", new PasswordResetReqDto { NewPassword = "x", ConfirmedPassword = "x" });

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
