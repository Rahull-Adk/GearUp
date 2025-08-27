using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.Cars
{
    public class Appointment
    {
        public Guid Id { get; private set; }
        public Guid AgentId { get; private set; }
        public Guid RequesterId { get; private set; }
        public DateTime Schedule { get; private set; }
        public string Location { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public string Notes { get; private set; }
        public User Agent { get; private set; }
        public User Requester { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Appointment()
        {
           Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Appointment CreateAppointment(
            Guid agentId,
            Guid requesterId,
            DateTime schedule,
            string location,
            string notes)
        {
            return new Appointment
            {
                AgentId = agentId,
                RequesterId = requesterId,
                Schedule = schedule,
                Location = location,
                Notes = notes,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

    }
}
