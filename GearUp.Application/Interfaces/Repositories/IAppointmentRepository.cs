using GearUp.Application.ServiceDtos.Appointment;
using GearUp.Domain.Entities.Cars;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IAppointmentRepository
    {
        Task AddAsync(Appointment appointment);
        Task<Appointment?> GetByIdAsync(Guid appointmentId);
        Task<List<AppointmentResponseDto>> GetByDealerIdAsync(Guid dealerId);
        Task<List<AppointmentResponseDto>> GetByRequesterIdAsync(Guid requesterId);
        Task<bool> HasCompletedAppointmentWithDealerAsync(Guid requesterId, Guid dealerId);
    }
}
