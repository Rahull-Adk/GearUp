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
        public async Task<IActionResult> GetAllPosts([FromQuery] int pageNumber = 1)
        {
            var currUserId = User.FindFirst(u => u.Type == "id")?.Value ?? Guid.Empty.ToString();
            var pageResult = await _postService.GetAllPostsAsync(Guid.Parse(currUserId), pageNumber);
            return Ok(pageResult);

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


    }

}
