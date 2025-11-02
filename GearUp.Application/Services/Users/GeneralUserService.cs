using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Users
{
    public sealed class GeneralUserService : IGeneralUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly ILogger<GeneralUserService> _logger;
        public GeneralUserService(IUserRepository userRepo, IMapper mapper, ICacheService cache, ILogger<GeneralUserService> logger)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }
        public async Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId)
        {
            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RegisterResponseDto>.Failure("User ID cannot be empty", 400);
            }

            var cacheKey = $"user:profile:{userId}";
            var cachedUser = await _cache.GetAsync<RegisterResponseDto>(cacheKey);

            if (cachedUser != null)
            {
                return Result<RegisterResponseDto>.Success(cachedUser, "User fetched Successfully from cache", 200);
            }

            var guidId = Guid.Parse(userId);

            var user = await _userRepo.GetUserByIdAsync(guidId);
            if (user == null)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);
            await _cache.SetAsync(cacheKey, mappedUser);
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

            var cacheKey = $"user:profile:{username}";
            var cachedUser = await _cache.GetAsync<RegisterResponseDto>(cacheKey);

            if (cachedUser != null)
            {
                return Result<RegisterResponseDto>.Success(cachedUser, "User fetched Successfully from cache", 200);
            }


            var user = await _userRepo.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Result<RegisterResponseDto>.Failure("User not found", 404);
            }

            var mappedUser = _mapper.Map<RegisterResponseDto>(user);

            await _cache.SetAsync(cacheKey, mappedUser);
            _logger.LogInformation("User profile fetched successfully for username: {Username}", username);
            return Result<RegisterResponseDto>.Success(mappedUser, "User fetched Successfully", 200);
        }

    }
}
