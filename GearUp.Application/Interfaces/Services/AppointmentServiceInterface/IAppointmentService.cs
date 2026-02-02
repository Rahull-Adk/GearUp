using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Appointment;

namespace GearUp.Application.Interfaces.Services.AppointmentServiceInterface
{
    public interface IAppointmentService
    {
        Task<Result<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentRequestDto dto, Guid requesterId);
        Task<Result<AppointmentResponseDto>> GetAppointmentByIdAsync(Guid appointmentId, Guid userId);
        Task<Result<CursorPageResult<AppointmentResponseDto>>> GetDealerAppointmentsAsync(Guid dealerId, string? cursor);
        Task<Result<CursorPageResult<AppointmentResponseDto>>> GetCustomerAppointmentsAsync(Guid customerId, string? cursor);
        Task<Result<AppointmentResponseDto>> AcceptAppointmentAsync(Guid appointmentId, Guid dealerId);
        Task<Result<AppointmentResponseDto>> RejectAppointmentAsync(Guid appointmentId, Guid dealerId, string? reason);
        Task<Result<AppointmentResponseDto>> CancelAppointmentAsync(Guid appointmentId, Guid userId);
        Task<Result<AppointmentResponseDto>> CompleteAppointmentAsync(Guid appointmentId, Guid dealerId);
    }
}
