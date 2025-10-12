using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/auth")]
    [EnableRateLimiting("Fixed")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRegisterService _registerService;
        private readonly ITokenGenerator _token;
        public AuthController(IRegisterService registerService, ITokenGenerator token)
        {
            _registerService = registerService;
            _token = token;
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

        [HttpPost("login")]
        public string LoginUser()
        {
            return "Hi";
        }
    }
}
