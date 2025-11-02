using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.User;
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
        public UserController(IKycService kycService, IGeneralUserService generalUserService, IProfileUpdateService profileUpdateService)
        {
            _kycService = kycService;
            _generalUserService = generalUserService;
            _profileUpdateService = profileUpdateService;
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
