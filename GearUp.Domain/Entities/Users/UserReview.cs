

namespace GearUp.Domain.Entities.Users
{
    public class UserReview
    {
        public Guid Id { get; private set; }
        public Guid ReviewerId { get; private set; }
        public Guid RevieweeId { get; private set; }
        public User? Reviewer { get; private set; }
        public User? Reviewee { get; private set; }
        public string ReviewText { get; private set; }
        public int Rating { get; private set; } 
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        private UserReview()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public static UserReview Create(Guid reviewerId, Guid revieweeId, string reviewText, int rating)
        {
            if (reviewerId == Guid.Empty)
                throw new ArgumentException("Reviewer ID cannot be empty.", nameof(reviewerId));
            if (revieweeId == Guid.Empty)
                throw new ArgumentException("Reviewee ID cannot be empty.", nameof(revieweeId));
            if (string.IsNullOrWhiteSpace(reviewText))
                throw new ArgumentException("Review text cannot be null or empty.", nameof(reviewText));
            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
            return new UserReview
            {
                ReviewerId = reviewerId,
                RevieweeId = revieweeId,
                ReviewText = reviewText,
                Rating = rating
            };
        }
        public void Update(string reviewText, int rating)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
                throw new ArgumentException("Review text cannot be null or empty.", nameof(reviewText));
            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
            ReviewText = reviewText;
            Rating = rating;
            UpdatedAt = DateTime.UtcNow;
        }




    }

}
