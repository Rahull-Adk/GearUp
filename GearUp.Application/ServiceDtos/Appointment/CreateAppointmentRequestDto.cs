namespace GearUp.Application.ServiceDtos.Appointment
{
    public class CreateAppointmentRequestDto
    {
        public Guid AgentId { get; set; }
        public Guid? CarId { get; set; }
        public DateTime Schedule { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
