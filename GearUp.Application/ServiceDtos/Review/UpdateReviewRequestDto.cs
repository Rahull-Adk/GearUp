namespace GearUp.Application.ServiceDtos.Review
{
    public class UpdateReviewRequestDto
    {
        public string ReviewText { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
