using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Appointment;
using GearUp.Domain.Entities.Cars;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IAppointmentRepository
    {
        Task AddAsync(Appointment appointment);
        Task<Appointment?> GetByIdAsync(Guid appointmentId);
        Task<CursorPageResult<AppointmentResponseDto>> GetByDealerIdAsync(Guid dealerId, Cursor? cursor);
        Task<CursorPageResult<AppointmentResponseDto>> GetByRequesterIdAsync(Guid requesterId, Cursor? cursor);
        Task<bool> HasCompletedAppointmentWithDealerAsync(Guid requesterId, Guid dealerId);
    }
}
