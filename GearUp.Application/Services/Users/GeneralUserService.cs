using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Users
{
    public sealed class GeneralUserService : IGeneralUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<GeneralUserService> _logger;
        public GeneralUserService(IUserRepository userRepo, IMapper mapper, ILogger<GeneralUserService> logger)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId)
        {
            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
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

            _logger.LogInformation("User profile fetched successfully for user ID: {UserId}", userId);
            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);
        }

        public async Task<Result<RegisterResponseDto>> GetUserProfile(string username)
        {
            _logger.LogInformation("Fetching profile for username: {Username}", username);
            if (string.IsNullOrEmpty(username))
            {
                return Result<RegisterResponseDto>.Failure("Username cannot be empty", 400);
            }


            var user = await _userRepo.GetUserByUsernameAsync(username);
            if (user == null || user.Role == UserRole.Admin)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);

            _logger.LogInformation("User profile fetched successfully for username: {Username}", username);
            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);

        }

    }
}
