using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography.Pkcs;


namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/auth")]
    [EnableRateLimiting("Fixed")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRegisterService _registerService;
        private readonly ILoginService _loginService;
        private readonly ILogoutService _logoutService;
        private readonly IEmailVerificationService _emailVerificationService;
        public AuthController(IRegisterService registerService, IEmailVerificationService emailVerificationService, ILoginService loginService, ILogoutService logoutService)
        {
            _registerService = registerService;
            _loginService = loginService;
            _logoutService = logoutService;
            _emailVerificationService = emailVerificationService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterRequestDto registerRequestDto)
        {
            var result = await _registerService.RegisterUser(registerRequestDto);
            return StatusCode(result.Status, result.ToApiResponse());
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginRequestDto req)
        {
            var result = await _loginService.LoginUser(req);
            if (!result.IsSuccess || result.Data.AccessToken == null || result.Data.RefreshToken == null)
            {
                return StatusCode(result.Status, result.ToApiResponse());
            }
            Response.Cookies.Append("access_token", result.Data?.AccessToken!, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", result.Data?.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return StatusCode(result.Status, new { message = "Login Successful" });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutUser()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            await _logoutService.Logout(refreshToken!);
            Response.Cookies.Delete("access_token");
            return StatusCode(200, new {message = "Logged out successfully"});
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _emailVerificationService.VerifyEmail(token);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            var result = await _loginService.RotateRefreshToken(refreshToken!);
                if (!result.IsSuccess || result.Data.AccessToken == null || result.Data.RefreshToken == null)
                {
                    return StatusCode(result.Status, result.ToApiResponse());
                }
                Response.Cookies.Append("access_token", result.Data?.AccessToken!, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                });
                Response.Cookies.Append("refresh_token", result.Data?.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                return StatusCode(result.Status, new { message = "Token refreshed successfully" });
            }
    }
}
