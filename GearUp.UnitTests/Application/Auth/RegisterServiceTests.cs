using FluentValidation;
using FluentValidation.Results;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Repositories; 
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Auth
{
    /// <summary>
    /// Unit tests for <see cref="RegisterService"/>.
    ///
    /// This suite documents the service's expected registration flow:
    /// validation is performed first, then email and username uniqueness are checked,
    /// and only successful registrations create a user, persist it, generate an email
    /// verification token, and publish a verification email message.
    /// </summary>
    public class RegisterServiceTests
    {
        private readonly Mock<IValidator<RegisterRequestDto>> _validator = new();
        /// <summary>
        /// Mocked user repository used to simulate duplicate checks and persistence.
        /// </summary>
        private readonly Mock<IUserRepository> _userRepo = new();
        /// <summary>
        /// Mocked password hasher used to verify that registration stores a hashed password.
        /// </summary>
        private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
        /// <summary>
        /// Mocked message publisher used to validate the verification-email dispatch.
        /// </summary>
        private readonly Mock<IMessagePublisher> _messagePublisher = new();
        /// <summary>
        /// Mocked token generator used to produce the email verification token.
        /// </summary>
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        /// <summary>
        /// Logger dependency passed into the service to keep the constructor fully populated.
        /// </summary>
        private readonly Mock<ILogger<RegisterService>> _logger = new();

        /// <summary>
        /// Builds a fresh <see cref="RegisterService"/> instance with the common test doubles.
        /// The password hasher is preconfigured because every successful registration is expected
        /// to hash the incoming password before persistence.
        /// </summary>
        private RegisterService CreateService()
        {

            _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                           .Returns("hashed_password");

            return new RegisterService(
                _validator.Object,
                _userRepo.Object,
                _passwordHasher.Object,
                _messagePublisher.Object,
                _tokenGenerator.Object,
                _logger.Object
            );
        }

        private static ValidationResult Valid() => new ValidationResult();

        /// <summary>
        /// Verifies that the service rejects registrations when validation fails.
        /// </summary>
        [Fact]
        public async Task Register_Fails_WhenValidationInvalid()
        {
            // Arrange
            var req = new RegisterRequestDto();
            var invalid = new ValidationResult(new[] { new ValidationFailure("Username", "Username is required") });
            _validator.Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(invalid);
            var svc = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => svc.RegisterUser(req));
            _userRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// Verifies that the service rejects registrations when the email is already taken.
        /// </summary>
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
            _validator.Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync(req.Email)).ReturnsAsync(User.CreateLocalUser("existing", req.Email, "Existing User"));

            var svc = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.ValidationException>(() => svc.RegisterUser(req));
            _userRepo.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Verifies that the service rejects registrations when the username is already taken.
        /// </summary>
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
            _validator.Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync(req.Email)).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.GetUserEntityByUsernameAsync(req.Username)).ReturnsAsync(User.CreateLocalUser(req.Username, "another@example.com", "Someone"));

            var svc = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.ValidationException>(() => svc.RegisterUser(req));
            _userRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// Verifies the full happy path: the user is created, persisted, a verification token is
        /// generated, and a verification email message is published to the expected queue.
        /// </summary>
        [Fact]
        public async Task Register_PublishesVerificationEmail_WhenRegistrationSucceeds()
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

            _validator.Setup(v => v.ValidateAsync(req, It.IsAny<CancellationToken>())).ReturnsAsync(Valid());
            _userRepo.Setup(r => r.GetUserEntityByEmailAsync(req.Email)).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.GetUserEntityByUsernameAsync(req.Username)).ReturnsAsync((User?)null);
            _userRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _tokenGenerator.Setup(t => t.GenerateEmailVerificationToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
                .Returns("verification-token");
            _messagePublisher.Setup(p => p.PublishAsync(It.IsAny<EmailRequestMessage>(), "gearup.email.queue", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var svc = CreateService();

            // Act
            var result = await svc.RegisterUser(req);

            // Assert
            Assert.True(result.IsSuccess);
            _userRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
            _messagePublisher.Verify(p => p.PublishAsync(It.IsAny<EmailRequestMessage>(), "gearup.email.queue", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
