using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRegisterService _registerService;
        public AuthController(IRegisterService registerService)
        {
            _registerService = registerService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterRequestDto registerRequestDto)
        {
                var result = await _registerService.RegisterUser(registerRequestDto);
                if (!result.IsSuccess)
                {
                    return BadRequest(result.ToApiResponse());
                }
                return Ok(result.ToApiResponse());
            
        }
    }
}
