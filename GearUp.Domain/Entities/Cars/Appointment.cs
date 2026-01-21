using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.Cars
{
    public class Appointment
    {
        public Guid Id { get; private set; }
        public Guid AgentId { get; private set; }
        public Guid RequesterId { get; private set; }
        public Guid? CarId { get; private set; }
        public DateTime Schedule { get; private set; }
        public string Location { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public string Notes { get; private set; }
        public string? RejectionReason { get; private set; }
        public User? Agent { get; private set; }
        public User? Requester { get; private set; }
        public Car? Car { get; private set; }
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
            string notes,
            Guid? carId = null)
        {
            return new Appointment
            {
                AgentId = agentId,
                RequesterId = requesterId,
                Schedule = schedule,
                Location = location,
                Notes = notes,
                CarId = carId,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void AcceptAppointment()
        {
            if (Status != AppointmentStatus.Pending)
                throw new InvalidOperationException("Only pending appointments can be accepted.");

            Status = AppointmentStatus.Scheduled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RejectAppointment(string? reason = null)
        {
            if (Status != AppointmentStatus.Pending)
                throw new InvalidOperationException("Only pending appointments can be rejected.");

            Status = AppointmentStatus.Rejected;
            RejectionReason = reason;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CancelAppointment()
        {
            if (Status == AppointmentStatus.Completed || Status == AppointmentStatus.Rejected)
                throw new InvalidOperationException("Completed or rejected appointments cannot be cancelled.");

            Status = AppointmentStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CompleteAppointment()
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new InvalidOperationException("Only scheduled appointments can be completed.");

            Status = AppointmentStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkNoShow()
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new InvalidOperationException("Only scheduled appointments can be marked as no-show.");

            Status = AppointmentStatus.NoShow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
