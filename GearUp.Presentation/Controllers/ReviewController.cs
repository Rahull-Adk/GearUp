using GearUp.Application.Interfaces.Services.ReviewServiceInterface;
using GearUp.Application.ServiceDtos.Review;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequestDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _reviewService.CreateReviewAsync(dto, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpPut("{reviewId:guid}")]
        public async Task<IActionResult> UpdateReview([FromRoute] Guid reviewId, [FromBody] UpdateReviewRequestDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _reviewService.UpdateReviewAsync(reviewId, dto, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpDelete("{reviewId:guid}")]
        public async Task<IActionResult> DeleteReview([FromRoute] Guid reviewId)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _reviewService.DeleteReviewAsync(reviewId, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("{reviewId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewById([FromRoute] Guid reviewId)
        {
            var result = await _reviewService.GetReviewByIdAsync(reviewId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("dealer/{dealerId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDealerReviews([FromRoute] Guid dealerId)
        {
            var result = await _reviewService.GetDealerReviewsAsync(dealerId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("dealer/{dealerId:guid}/summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDealerRatingSummary([FromRoute] Guid dealerId)
        {
            var result = await _reviewService.GetDealerRatingSummaryAsync(dealerId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _reviewService.GetMyReviewsAsync(userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
