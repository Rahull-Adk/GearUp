using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Enums;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/users")]
    [EnableRateLimiting("Fixed")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IKycService _kycService;
        private readonly IGeneralUserService _generalUserService;
        private readonly IProfileUpdateService _profileUpdateService;
        private readonly IPostService _postService;
        private readonly ICarService _carService;
        public UserController(IKycService kycService, IGeneralUserService generalUserService, IProfileUpdateService profileUpdateService, IPostService postService, ICarService carService)
        {
            _kycService = kycService;
            _generalUserService = generalUserService;
            _profileUpdateService = profileUpdateService;
            _postService = postService;
            _carService = carService;
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _generalUserService.GetCurrentUserProfileService(userId!);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize]
        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            var result = await _generalUserService.GetUserProfile(username);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequestDto updateUserDto)
        {
            var id = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _profileUpdateService.UpdateUserProfileService(id!, updateUserDto);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize]
        [HttpGet("{dealerId:guid}/posts")]
        public async  Task<IActionResult> GetDealerPosts([FromRoute] Guid dealerId,[FromQuery] int pageNum = 1)
        {
            var result = await _generalUserService.GetPostsByDealerId(dealerId, pageNum);
            return StatusCode(result.Status, result);
        }


        [Authorize]
        [HttpGet("{dealerId:guid}/cars")]
        public async Task<IActionResult> GetMyCars([FromRoute] Guid dealerId, [FromQuery] int pageNum)
        {
            var result = await _carService.GetDealerCarsAsync(dealerId, pageNum);
            return  StatusCode(result.Status, result.ToApiResponse());
        }


        [Authorize(Policy = "CustomerOnly")]
        [HttpPost("kyc")]
        public async Task<IActionResult> SubmitKycDocuments([FromForm] KycRequestDto kycDocumentDto)
        {
            var userId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _kycService.SubmitKycService(userId!, kycDocumentDto);
            return StatusCode(result.Status, result.ToApiResponse());
        }

    }
}
