using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/comments")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILikeService _likeService;

        public CommentController(ICommentService commentService, ILikeService likeService)
        {
            _commentService = commentService;
            _likeService = likeService;
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var rawId = User.FindFirst(u => u.Type == "id")?.Value;
            return !string.IsNullOrWhiteSpace(rawId) && Guid.TryParse(rawId, out userId);
        }

        [Authorize]
        [HttpPost("{commentId:guid}/like")]
        public async Task<IActionResult> LikeComment([FromRoute] Guid commentId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _likeService.LikeCommentAsync(commentId, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpDelete("{commentId:guid}/like")]
        public async Task<IActionResult> UnlikeComment([FromRoute] Guid commentId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _likeService.UnlikeCommentAsync(commentId, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CommentOnPost([FromBody] CreateCommentDto comment)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _commentService.PostCommentAsync(comment, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPut("{commentId:guid}")]
        public async Task<IActionResult> UpdateComment([FromRoute] Guid commentId, [FromBody] string comment)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _commentService.UpdateCommentAsync(commentId, currentUserId, comment);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpDelete("{commentId:guid}")]
        public async Task<IActionResult> DeleteComment([FromRoute] Guid commentId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _commentService.DeleteCommentAsync(commentId, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpGet("{postId:guid}/top")]
        public async Task<IActionResult> GetTopLevelCommentsByPostId([FromRoute] Guid postId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _commentService.GetParentCommentsByPostId(postId, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpGet("{parentCommentId:guid}/childrens")]
        public async Task<IActionResult> GetChildCommentsByParentId([FromRoute] Guid parentCommentId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return BadRequest(new { message = "Invalid user id claim." });
            }
            var result = await _commentService.GetChildCommentsByParentId(parentCommentId, currentUserId);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpGet("{commentId:guid}/likers")]
        public async Task<IActionResult> GetCommentLikers([FromRoute] Guid commentId, [FromQuery] string? cursor)
        {
            var result = await _commentService.GetCommentLikersAsync(commentId, cursor);
            return StatusCode(result.Status, result);
        }
    }
}
