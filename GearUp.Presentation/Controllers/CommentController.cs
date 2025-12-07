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

        [Authorize]
        [HttpPost("{commentId:guid}/like")]
        public async Task<IActionResult> LikeComment([FromRoute] Guid commentId)
        {
            var currentUser = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _likeService.LikeCommentAsync(commentId, Guid.Parse(currentUser!));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CommentOnPost([FromBody] CreateCommentDto comment)
        {
            var currentUser = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _commentService.PostCommentAsync(comment, Guid.Parse(currentUser!));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPut("{commentId:guid}")]
        public async Task<IActionResult> UpdateComment([FromRoute] Guid commentId, [FromBody] string comment)
        {
            var currentUser = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _commentService.UpdateCommentAsync(commentId, Guid.Parse(currentUser!), comment);
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpDelete("{commentId:guid}")]
        public async Task<IActionResult> DeleteComment([FromRoute] Guid commentId)
        {
            var currentUser = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _commentService.DeleteCommentAsync(commentId, Guid.Parse(currentUser!));
            return StatusCode(result.Status, result);
        }


    }
}
