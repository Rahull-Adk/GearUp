using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ILoginService _loginService;
        private readonly IGeneralAdminService _generalAdminService;
        public AdminController(ILoginService loginService, IGeneralAdminService generalAdminService)
        {
            _loginService = loginService;
            _generalAdminService = generalAdminService;
        }

        private static CookieOptions BuildAccessTokenCookieOptions() => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        private static CookieOptions BuildRefreshTokenCookieOptions() => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var rawId = User.FindFirst(c => c.Type == "id")?.Value;
            return !string.IsNullOrWhiteSpace(rawId) && Guid.TryParse(rawId, out userId);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequestDto request)
        {
            var result = await _loginService.LoginAdmin(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.Status, result.ToApiResponse());
            }
            Response.Cookies.Append("access_token", result.Data?.AccessToken!, BuildAccessTokenCookieOptions());
            Response.Cookies.Append("refresh_token", result.Data?.RefreshToken!, BuildRefreshTokenCookieOptions());

            return StatusCode(result.Status, result.ToApiResponse());
        }

        #region KYC Endpoints

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc")]
        public async Task<IActionResult> GetKycRequests([FromQuery] string? cursor)
        {
            if (!TryGetCurrentUserId(out var adminUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _generalAdminService.GetAllKycs(adminUserId, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc/{kycId:guid}")]
        public async Task<IActionResult> GetKycRequestById([FromRoute] Guid kycId)
        {
            var result = await _generalAdminService.GetKycById(kycId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("kyc/{kycId:guid}")]
        public async Task<IActionResult> ReviewKyc([FromRoute] Guid kycId, [FromBody] kycRequestDto req)
        {
            if (!TryGetCurrentUserId(out var reviewerId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _generalAdminService.UpdateKycStatus(kycId, req.Status, reviewerId, req.RejectionReason);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc/status/{status}")]
        public async Task<IActionResult> GetKycRequestsByStatus([FromRoute] KycStatus status, [FromQuery] string? cursor)
        {
            if (!TryGetCurrentUserId(out var adminUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _generalAdminService.GetKycsByStatus(adminUserId, status, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        #endregion

        #region Car Endpoints

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars")]
        public async Task<IActionResult> GetAllCars([FromQuery] string? cursor)
        {
            var result = await _generalAdminService.GetAllCars(cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars/{carId:guid}")]
        public async Task<IActionResult> GetCarById([FromRoute] Guid carId)
        {
            var result = await _generalAdminService.GetCarById(carId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars/dealer/{dealerId:guid}")]
        public async Task<IActionResult> GetCarsByDealerId([FromRoute] Guid dealerId, [FromQuery] string? cursor)
        {
            var result = await _generalAdminService.GetCarsByDealerId(dealerId, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars/status/{status}")]
        public async Task<IActionResult> GetCarsByValidationStatus([FromRoute] CarValidationStatus status, [FromQuery] string? cursor)
        {
            var result = await _generalAdminService.GetCarsByValidationStatus(status, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("cars/{carId:guid}")]
        public async Task<IActionResult> ReviewCar([FromRoute] Guid carId, [FromBody] CarValidationRequestDto req)
        {
            if (!TryGetCurrentUserId(out var reviewerId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _generalAdminService.UpdateCarValidationStatus(carId, req.Status, reviewerId, req.RejectionReason);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        #endregion
    }
}
