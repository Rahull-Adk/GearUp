using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities;
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
            return StatusCode(result.Status, result.ToApiResponse());
        }
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc")]
        public async Task<IActionResult> GetKycRequests()
        {
            var result = await _generalAdminService.GetAllKycs();
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("kyc/{kycId}")]
        public async Task<IActionResult> GetKycRequestById([FromRoute] Guid kycId)
        {
            var result = await _generalAdminService.GetKycById(kycId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("kyc/{kycId}")]
        public async Task<IActionResult> ReviewKyc([FromRoute] Guid kycId, kycRequestDto req)
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

    }
}
