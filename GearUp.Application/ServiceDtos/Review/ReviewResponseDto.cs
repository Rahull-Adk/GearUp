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
        public string ReviewText { get; set; } = string.Empty;
        public double Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
