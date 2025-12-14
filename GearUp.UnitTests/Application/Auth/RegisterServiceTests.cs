using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Auth
{
    public class RegisterServiceTests
    {
        private readonly Mock<IValidator<RegisterRequestDto>> _validator = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<RegisterService>> _logger = new();

        private RegisterService CreateService() => new(
        _validator.Object,
        _userRepo.Object,
        _passwordHasher.Object,
        _emailSender.Object,
        _tokenGenerator.Object,
        _mapper.Object,
        _logger.Object
        );

        private static ValidationResult Valid() => new ValidationResult();

        [Fact]
        public async Task Register_Success_ReturnsCreatedAndSendsEmail()
        {
            // Arrange
            var req = new RegisterRequestDto
            {
                Username = "john",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            _validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.GetUserByUsernameAsync(req.Username)).ReturnsAsync((User?)null);
            _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), req.Password)).Returns("hashed");
            _tokenGenerator.Setup(t => t.GenerateEmailVerificationToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("email-token");

            var expected = new RegisterResponseDto(Guid.NewGuid(), null, req.Username, req.Email, "John Doe", "Customer", DateOnly.FromDayNumber(1), "123", "https://i.pravatar.cc/300");
            _mapper.Setup(m => m.Map<RegisterResponseDto>(It.IsAny<User>())).Returns(expected);

            var svc = CreateService();

            // Act
            var result = await svc.RegisterUser(req);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.Status);
            Assert.Equal(expected, result.Data);

            _userRepo.Verify(r => r.AddUserAsync(It.Is<User>(u => u.Email == req.Email && u.Username == req.Username && u.PasswordHash == "hashed")), Times.Once);
            _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _emailSender.Verify(e => e.SendVerificationEmail(req.Email, "email-token"), Times.Once);
        }

        [Fact]
        public async Task Register_Fails_WhenValidationInvalid()
        {
            // Arrange
            var req = new RegisterRequestDto();
            var invalid = new ValidationResult(new[] { new ValidationFailure("Username", "Username is required") });
            _validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(invalid);
            var svc = CreateService();

            // Act
            var result = await svc.RegisterUser(req);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
            _userRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Register_Fails_WhenEmailExists()
        {
            // Arrange
            var req = new RegisterRequestDto
            {
                Username = "john",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };
            _validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(User.CreateLocalUser("existing", req.Email, "Existing User"));

            var svc = CreateService();

            // Act
            var result = await svc.RegisterUser(req);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
            _userRepo.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Register_Fails_WhenUsernameExists()
        {
            // Arrange
            var req = new RegisterRequestDto
            {
                Username = "john",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };
            _validator.Setup(v => v.ValidateAsync(req, default)).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.GetUserByUsernameAsync(req.Username)).ReturnsAsync(User.CreateLocalUser(req.Username, "another@example.com", "Someone"));

            var svc = CreateService();

            // Act
            var result = await svc.RegisterUser(req);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
            _userRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
