namespace GearUp.Application.ServiceDtos.Review
{
    public class DealerRatingSummaryDto
    {
        public Guid DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        // Count of reviews in each star range (e.g., 4.5-5.0 = FiveStar, 3.5-4.49 = FourStar, etc.)
        public int FiveStarCount { get; set; }   // Rating >= 4.5
        public int FourStarCount { get; set; }   // Rating >= 3.5 && < 4.5
        public int ThreeStarCount { get; set; }  // Rating >= 2.5 && < 3.5
        public int TwoStarCount { get; set; }    // Rating >= 1.5 && < 2.5
        public int OneStarCount { get; set; }    // Rating < 1.5
    }
}
