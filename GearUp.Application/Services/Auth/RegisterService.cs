using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace GearUp.Application.Services.Auth
{
    public class RegisterService : IRegisterService
    {
        private readonly IValidator<RegisterRequestDto> _validator;
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegisterService(IValidator<RegisterRequestDto> validator, IUserRepository userRepo, IPasswordHasher<User> passwordHasher)
        {
            _validator = validator;
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
        }
        public async Task<Result<RegisterResponseDto>> RegisterUser(RegisterRequestDto data)
        {
          
                var validationResult = await _validator.ValidateAsync(data);

                if(!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return Result<RegisterResponseDto>.Failure(errors);
                }

                var isEmailExists = await _userRepo.GetUserByEmailAsync(data.Email);
                if(isEmailExists != null)
                {
                    return Result<RegisterResponseDto>.Failure("Account with this email already exists. Please login");
                }
                var isUsernameExists = await _userRepo.GetUserByUsernameAsync(data.Username);
                if(isUsernameExists != null)
                {
                    return Result<RegisterResponseDto>.Failure("Account with this username already exists. Please choose another username");
                }
                
               
                var newUser = User.CreateLocalUser(data.Username, data.Email, data.FirstName + " " + data.LastName);
                var hashedPassword = _passwordHasher.HashPassword(newUser, data.Password);
                newUser.SetPassword(hashedPassword);

                await _userRepo.AddUserAsync(newUser);
                await _userRepo.SaveChangesAsync();

                return Result<RegisterResponseDto>.Success(new RegisterResponseDto
                {
                    UserId = newUser.Id,
                    Email = newUser.Email,
                    Username = newUser.Username,
                    Name = newUser.Name
                });
        }
    }
}
