using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Appointment;

namespace GearUp.Application.Interfaces.Services.AppointmentServiceInterface
{
    public interface IAppointmentService
    {
        Task<Result<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentRequestDto dto, Guid requesterId);
        Task<Result<AppointmentResponseDto>> GetAppointmentByIdAsync(Guid appointmentId, Guid userId);
        Task<Result<List<AppointmentResponseDto>>> GetDealerAppointmentsAsync(Guid dealerId);
        Task<Result<List<AppointmentResponseDto>>> GetCustomerAppointmentsAsync(Guid customerId);
        Task<Result<AppointmentResponseDto>> AcceptAppointmentAsync(Guid appointmentId, Guid dealerId);
        Task<Result<AppointmentResponseDto>> RejectAppointmentAsync(Guid appointmentId, Guid dealerId, string? reason);
        Task<Result<AppointmentResponseDto>> CancelAppointmentAsync(Guid appointmentId, Guid userId);
        Task<Result<AppointmentResponseDto>> CompleteAppointmentAsync(Guid appointmentId, Guid dealerId);
    }
}
