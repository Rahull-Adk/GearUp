namespace GearUp.Application.ServiceDtos.Review
{
    public class DealerRatingSummaryDto
    {
        public Guid DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }
}
