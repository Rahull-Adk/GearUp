namespace GearUp.Application.ServiceDtos.Review
{
    public class CreateReviewRequestDto
    {
        public Guid DealerId { get; set; }
        public string ReviewText { get; set; } = string.Empty;
        public double Rating { get; set; }
    }
}
