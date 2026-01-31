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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequestDto request)
        {
            var result = await _loginService.LoginAdmin(request);
            if (!result.IsSuccess)
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

            return StatusCode(result.Status, result.ToApiResponse());
        }

        #region KYC Endpoints

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc")]
        public async Task<IActionResult> GetKycRequests()
        {
            var result = await _generalAdminService.GetAllKycs();
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
            var reviewerId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _generalAdminService.UpdateKycStatus(kycId, req.Status, Guid.Parse(reviewerId!), req.RejectionReason!);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc/status/{status}")]
        public async Task<IActionResult> GetKycRequestsByStatus([FromRoute] KycStatus status)
        {
            var result = await _generalAdminService.GetKycsByStatus(status);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        #endregion

        #region Car Endpoints

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars")]
        public async Task<IActionResult> GetAllCars([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _generalAdminService.GetAllCars(pageNum, pageSize);
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
        public async Task<IActionResult> GetCarsByDealerId([FromRoute] Guid dealerId, [FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _generalAdminService.GetCarsByDealerId(dealerId, pageNum, pageSize);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("cars/status/{status}")]
        public async Task<IActionResult> GetCarsByValidationStatus([FromRoute] CarValidationStatus status, [FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _generalAdminService.GetCarsByValidationStatus(status, pageNum, pageSize);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("cars/{carId:guid}")]
        public async Task<IActionResult> ReviewCar([FromRoute] Guid carId, [FromBody] CarValidationRequestDto req)
        {
            var reviewerId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _generalAdminService.UpdateCarValidationStatus(carId, req.Status, Guid.Parse(reviewerId!), req.RejectionReason);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        #endregion
    }
}
