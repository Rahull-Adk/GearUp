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
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _userService.GetCurrentUserProfileService(userId!);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequestDto updateUserDto)
        {
            var id = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _userService.UpdateUserProfileService(id!, updateUserDto);
            return StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
