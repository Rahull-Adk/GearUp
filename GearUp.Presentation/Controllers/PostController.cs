using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/posts")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILikeService _likeService;


        public PostController(IPostService postService, ILikeService likeService)
        {
            _postService = postService;
            _likeService = likeService;
        }

        [Authorize]
        [HttpGet("{postId:guid}")]
        public async Task<IActionResult> GetPostById([FromRoute] Guid postId)
        {
            var currUserId = User.FindFirst(u => u.Type == "id")?.Value ?? Guid.Empty.ToString();
            var result = await _postService.GetPostByIdAsync(postId, Guid.Parse(currUserId));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpGet("")]
        public async Task<IActionResult> GetFeed([FromQuery] string mode, [FromQuery] int pageNumber = 1)
        {
            var currUserId = User.FindFirst(u => u.Type == "id")?.Value ?? Guid.Empty.ToString();
            var pageResult = await _postService.GetLatestFeedAsync(Guid.Parse(currUserId), pageNumber);
            return Ok(pageResult);
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpGet("me")]

        public async Task<IActionResult> GetMyPosts([FromQuery] int pageNum = 1)
        {
            var currUserId = User.FindFirst(u => u.Type == "id")?.Value ?? Guid.Empty.ToString();
            var result = await _postService.GetMyPosts(Guid.Parse(currUserId), pageNum);
            return StatusCode(result.Status, result);
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpPost("")]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequestDto req)
        {
            var dealerId = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _postService.CreatePostAsync(req, Guid.Parse(dealerId!));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPost("{postId:guid}/like")]
        public async Task<IActionResult> LikePost([FromRoute] Guid postId)
        {
            var currenetUserId = User.FindFirst(u => u.Type == "id")?.Value;
            var result = await _likeService.LikePostAsync(postId, Guid.Parse(currenetUserId!));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpGet("{postId:guid}/like")]
        public async Task<IActionResult> GetLikedUsers([FromRoute] Guid postId, [FromQuery] int pageNum)
        {
            var likedUsers = await _postService.GetPostLikersAsync(postId, pageNum);
            return StatusCode(likedUsers.Status, likedUsers);
        }

        [Authorize]
        [HttpDelete("{postId:guid}")]
        public async Task<IActionResult> DeletePost([FromRoute] Guid postId)
        {
            var currentUserId = User.FindFirst((u => u.Type == "id"))?.Value;
            var result = await _postService.DeletePostAsync(postId, Guid.Parse(currentUserId!));
            return StatusCode(result.Status, result);
        }

        [Authorize]
        [HttpPut("{postId:guid}")]
        public async Task<IActionResult> UpdatePost([FromRoute] Guid postId, [FromBody] UpdatePostDto dto)
        {
            var currentUserId = User.FindFirst((u => u.Type == "id"))?.Value;
            var result = await _postService.UpdatePostAsync(postId, Guid.Parse(currentUserId!), dto);
            return StatusCode(result.Status, result);
        }
    }
}