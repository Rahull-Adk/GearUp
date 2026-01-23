namespace GearUp.Application.ServiceDtos.Review
{
    public class CreateReviewRequestDto
    {
        public Guid AppointmentId { get; set; }
        public string ReviewText { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
