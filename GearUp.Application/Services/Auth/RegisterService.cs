using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace GearUp.Application.Services.Auth
{
    public sealed class RegisterService : IRegisterService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ILogger<RegisterService> _logger;
        public RegisterService(IUserRepository userRepo, IPasswordHasher<User> passwordHasher, IMessagePublisher messagePublisher, ITokenGenerator tokenGenerator, ILogger<RegisterService> logger)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
            _messagePublisher = messagePublisher;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }
        public async Task<Result<RegisterResponseDto>> RegisterUser(RegisterRequestDto data)
        {
            _logger.LogInformation("Starting user registration for email: {Email}", data.Email);
            await _validator.EnsureValidAsync(data);

            var isEmailExists = await _userRepo.GetUserEntityByEmailAsync(data.Email);
            if(isEmailExists != null)
            {
                throw new Domain.Exceptions.ValidationException("Account with this email already exists. Please login");
            }

            var isUsernameExists = await _userRepo.GetUserEntityByUsernameAsync(data.Username);
            if(isUsernameExists != null)
            {
                throw new Domain.Exceptions.ValidationException("Account with this username already exists. Please choose another username");
            }


            var newUser = User.CreateLocalUser(data.Username, data.Email, data.FirstName + " " + data.LastName);
            var hashedPassword = _passwordHasher.HashPassword(newUser, data.Password);
            newUser.SetPassword(hashedPassword);
            await _userRepo.AddUserAsync(newUser);
            await _userRepo.SaveChangesAsync();

            var claims = new[]
            {
                new Claim("id", newUser.Id.ToString()),
                new Claim("email", newUser.Email),
                new Claim("role", newUser.Role.ToString()),
                new Claim("purpose", "email_verification")
            };

            var emailVerificationToken = _tokenGenerator.GenerateEmailVerificationToken(claims);

            var emailMessage = new EmailRequestMessage
            {
                CorrelationId = newUser.Id.ToString(),
                ToEmail = newUser.Email,
                TemplateName = "VerifyEmail",
                Payload = new Dictionary<string, string>
                {
                    ["token"] = emailVerificationToken
                }
            };

            await _messagePublisher.PublishAsync(emailMessage, "gearup.email.queue");
            _logger.LogInformation("User registration successful for email: {Email}", data.Email);
            return Result<RegisterResponseDto>.Success(null!, "User created Successfully!", 201);
        }
    }
}