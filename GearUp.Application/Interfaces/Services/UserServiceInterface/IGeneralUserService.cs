using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Interfaces.Services.UserServiceInterface
{
    public interface IGeneralUserService
    {
        Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId);
        Task<Result<RegisterResponseDto>> GetUserProfile(string username);
        Task<Result<CursorPageResult<PostResponseDto?>>> GetPostsByDealerId(Guid dealerId, string? cursor);
    }
}
