using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace GearUp.Application.Services.Users
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly ITokenGenerator _tokenGenerator;
        public UserService(IUserRepository userRepo, IMapper mapper, IPasswordHasher<User> passwordHasher, IEmailSender emailSender, ITokenGenerator tokenGenerator)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _tokenGenerator = tokenGenerator;
        }
        public async Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RegisterResponseDto>.Failure("User ID cannot be empty", 400);
            }

            var guidId = Guid.Parse(userId);

            var user = await _userRepo.GetUserByIdAsync(guidId);
            if (user == null)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);

            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);
        }

        public async Task<Result<UpdateUserResponseDto>> UpdateUserProfileService(string userId, UpdateUserRequestDto reqDto)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<UpdateUserResponseDto>.Failure("Unauthorized", 401);
            }

            var user = await _userRepo.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return Result<UpdateUserResponseDto>.Failure("User not found", 404);
            }

            if (reqDto.Name != null && string.Equals(user.Name, reqDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UpdateUserResponseDto>.Failure("New name cannot be same with the old one.", 400);
            }

            if (reqDto.PhoneNumber != null && string.Equals(user.PhoneNumber, reqDto.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UpdateUserResponseDto>.Failure("New phone number cannot be same with the old one.", 400);
            }

            if (reqDto.DateOfBirth != null && reqDto.DateOfBirth == user.DateOfBirth)
            {
                return Result<UpdateUserResponseDto>.Failure("New date of birth cannot be same with the old one.", 400);
            }

            string? newHashedPassword = null;
            if (reqDto.CurrentPassword != null && reqDto.NewPassword != null && reqDto.ConfirmedNewPassword != null)
            {
                if (reqDto.NewPassword != reqDto.ConfirmedNewPassword)
                {
                    return Result<UpdateUserResponseDto>.Failure("New password and confirmed new password do not match", 400);
                }

                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, reqDto.CurrentPassword);

                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    return Result<UpdateUserResponseDto>.Failure("Invalid credentials", 400);
                }
                if (reqDto.CurrentPassword == reqDto.NewPassword)
                {
                    return Result<UpdateUserResponseDto>.Failure("New password cannot be same as the current password", 400);
                }
                newHashedPassword = _passwordHasher.HashPassword(user, reqDto.NewPassword);
            }

            if (reqDto.NewEmail != null)
            {
                if (!string.Equals(user.Email, reqDto.NewEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUserWithEmail = await _userRepo.GetUserByEmailAsync(reqDto.NewEmail);
                    if (existingUserWithEmail != null)
                    {
                        return Result<UpdateUserResponseDto>.Failure("Email is already in use", 400);
                    }
                    var claims = new[]
                    {
                    new Claim("id", user.Id.ToString()),
                    new Claim("email", reqDto.NewEmail),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("purpose", "email_reset_verification")
                    };

                    user.SetPendingEmail(reqDto.NewEmail);
                    user.SetIsPendingEmailVerified(false);
                    await _userRepo.SaveChangesAsync();
                    var emailVerificationToken = _tokenGenerator.GenerateEmailVerificationToken(claims);
                    await _emailSender.SendEmailReset(reqDto.NewEmail, emailVerificationToken);
                }
                else
                {
                    return Result<UpdateUserResponseDto>.Failure("New email cannot be same with the old one.", 400);
                }
            }

            user.UpdateProfile(
                reqDto.Name,
                reqDto.PhoneNumber,
                reqDto.AvatarUrl,
                reqDto.DateOfBirth,
                newHashedPassword
            );
            await _userRepo.SaveChangesAsync();
            var mappedUser = _mapper.Map<UpdateUserResponseDto>(user);
            return Result<UpdateUserResponseDto>.Success(mappedUser, "Please verify your new email", 200);
        }
    }
}
