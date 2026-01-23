namespace GearUp.Application.ServiceDtos.Review
{
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public Guid RevieweeId { get; set; }
        public string RevieweeName { get; set; } = string.Empty;
        public Guid AppointmentId { get; set; }
        public string ReviewText { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
