using GearUp.Application.Interfaces.Services.AppointmentServiceInterface;
using GearUp.Application.ServiceDtos.Appointment;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/appointments")]
    [ApiController]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.CreateAppointmentAsync(dto, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("{appointmentId:guid}")]
        public async Task<IActionResult> GetAppointmentById([FromRoute] Guid appointmentId)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.GetAppointmentByIdAsync(appointmentId, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpGet("dealer")]
        public async Task<IActionResult> GetDealerAppointments()
        {
            var dealerId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.GetDealerAppointmentsAsync(dealerId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetCustomerAppointments()
        {
            var customerId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.GetCustomerAppointmentsAsync(customerId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpPatch("{appointmentId:guid}/accept")]
        public async Task<IActionResult> AcceptAppointment([FromRoute] Guid appointmentId)
        {
            var dealerId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.AcceptAppointmentAsync(appointmentId, dealerId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpPatch("{appointmentId:guid}/reject")]
        public async Task<IActionResult> RejectAppointment([FromRoute] Guid appointmentId, [FromBody] UpdateAppointmentStatusDto dto)
        {
            var dealerId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.RejectAppointmentAsync(appointmentId, dealerId, dto.RejectionReason);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpPatch("{appointmentId:guid}/cancel")]
        public async Task<IActionResult> CancelAppointment([FromRoute] Guid appointmentId)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _appointmentService.CancelAppointmentAsync(appointmentId, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
