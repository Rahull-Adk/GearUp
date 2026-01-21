using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos.Appointment
{
    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public Guid RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public Guid? CarId { get; set; }
        public string? CarTitle { get; set; }
        public DateTime Schedule { get; set; }
        public string Location { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
