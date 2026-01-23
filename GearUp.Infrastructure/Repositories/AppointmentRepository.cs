using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Appointment;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly GearUpDbContext _db;

        public AppointmentRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Appointment appointment)
        {
            await _db.Appointments.AddAsync(appointment);
        }

        public async Task<Appointment?> GetByIdAsync(Guid appointmentId)
        {
            return await _db.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<List<AppointmentResponseDto>> GetByDealerIdAsync(Guid dealerId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Where(a => a.AgentId == dealerId)
                .Include(a => a.Agent)
                .Include(a => a.Requester)
                .Include(a => a.Car)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    AgentId = a.AgentId,
                    AgentName = a.Agent != null ? a.Agent.Name : "Unknown",
                    RequesterId = a.RequesterId,
                    RequesterName = a.Requester != null ? a.Requester.Name : "Unknown",
                    CarId = a.CarId,
                    CarTitle = a.Car != null ? a.Car.Title : null,
                    Schedule = a.Schedule,
                    Location = a.Location,
                    Status = a.Status,
                    Notes = a.Notes,
                    RejectionReason = a.RejectionReason,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<List<AppointmentResponseDto>> GetByRequesterIdAsync(Guid requesterId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Where(a => a.RequesterId == requesterId)
                .Include(a => a.Agent)
                .Include(a => a.Requester)
                .Include(a => a.Car)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    AgentId = a.AgentId,
                    AgentName = a.Agent != null ? a.Agent.Name : "Unknown",
                    RequesterId = a.RequesterId,
                    RequesterName = a.Requester != null ? a.Requester.Name : "Unknown",
                    CarId = a.CarId,
                    CarTitle = a.Car != null ? a.Car.Title : null,
                    Schedule = a.Schedule,
                    Location = a.Location,
                    Status = a.Status,
                    Notes = a.Notes,
                    RejectionReason = a.RejectionReason,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> HasCompletedAppointmentWithDealerAsync(Guid requesterId, Guid dealerId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .AnyAsync(a => a.RequesterId == requesterId
                            && a.AgentId == dealerId
                            && a.Status == AppointmentStatus.Completed);
        }
    }
}
