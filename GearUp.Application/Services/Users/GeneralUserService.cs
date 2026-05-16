using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Enums;
using GearUp.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Users
{
    public sealed class GeneralUserService : IGeneralUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPostRepository _postRepo;
        private readonly ILogger<GeneralUserService> _logger;
        public GeneralUserService(IUserRepository userRepo, IPostRepository postRepo, ILogger<GeneralUserService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
            _postRepo = postRepo;

        }
        public async Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId)
        {
            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                throw new Domain.Exceptions.ValidationException("User ID cannot be empty");
            }
            if (!Guid.TryParse(userId, out var guidId))
            {
                throw new Domain.Exceptions.ValidationException("Invalid user ID format");
            }

            var user = await _userRepo.GetUserByIdAsync(guidId)
                       ?? throw new NotFoundException("User", userId);

            _logger.LogInformation("User profile fetched successfully for user ID: {UserId}", userId);
            return Result<RegisterResponseDto>.Success(user, "User fetched Successfully", 200);
        }
        public async Task<Result<CursorPageResult<PostListResponseDto?>>> GetPostsByDealerId(Guid dealerId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching posts for user: {UserId}", dealerId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var postsPaged = await _postRepo.GetAllUserPostByUserIdAsync(cursor, dealerId, cancellationToken);

            _logger.LogInformation("Posts fetched successfully from database");

            return Result<CursorPageResult<PostListResponseDto?>>.Success(postsPaged, "Post fetched successfully.");
        }
        public async Task<Result<RegisterResponseDto>> GetUserProfile(string username)
        {
            _logger.LogInformation("Fetching profile for username: {Username}", username);
            if (string.IsNullOrEmpty(username))
            {
                throw new Domain.Exceptions.ValidationException("Username cannot be empty");
            }


            var user = await _userRepo.GetUserByUsernameAsync(username);
            if (user == null || user.Role == UserRole.Admin)
            {
                throw new NotFoundException("User", username);
            }
            _logger.LogInformation("User profile fetched successfully for username: {Username}", username);
            return Result<RegisterResponseDto>.Success(user, "User fetched Successfully", 200);

        }

    }
}
