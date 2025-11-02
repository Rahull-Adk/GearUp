using AutoMapper;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace GearUp.Application.Services.Auth
{
    public sealed class RegisterService : IRegisterService
    {
        private readonly IValidator<RegisterRequestDto> _validator;
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IMapper _mapper;
        private readonly ILogger<RegisterService> _logger;
        public RegisterService(IValidator<RegisterRequestDto> validator, IUserRepository userRepo, IPasswordHasher<User> passwordHasher, IEmailSender emailSender, ITokenGenerator tokenGenerator, IMapper mapper, ILogger<RegisterService> logger)
        {
            _validator = validator;
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _tokenGenerator = tokenGenerator;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<RegisterResponseDto>> RegisterUser(RegisterRequestDto data)
        {
                _logger.LogInformation("Starting user registration for email: {Email}", data.Email);
            var validationResult = await _validator.ValidateAsync(data);

                if(!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return Result<RegisterResponseDto>.Failure(errors, 400);
                }

                var isEmailExists = await _userRepo.GetUserByEmailAsync(data.Email);
                if(isEmailExists != null)
                {
                    return Result<RegisterResponseDto>.Failure("Account with this email already exists. Please login", 400);
                }
                var isUsernameExists = await _userRepo.GetUserByUsernameAsync(data.Username);
                if(isUsernameExists != null)
                {
                    return Result<RegisterResponseDto>.Failure("Account with this username already exists. Please choose another username", 400);
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

            await _emailSender.SendVerificationEmail(newUser.Email, emailVerificationToken);

            var mappedRes = _mapper.Map<RegisterResponseDto>(newUser);
            _logger.LogInformation("User registration successful for email: {Email}", data.Email);
            return Result<RegisterResponseDto>.Success(mappedRes, "User created Successfully!", 201);
        }
    }
}
