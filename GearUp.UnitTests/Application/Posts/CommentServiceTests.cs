using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.Services.Posts;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Posts
{
    public class CommentServiceTests
    {
        private readonly Mock<ILogger<ICommentService>> _logger = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<ICommonRepository> _commonRepository = new();
        private readonly Mock<IPostRepository> _postRepository = new();
        private readonly Mock<ICommentRepository> _commentRepository = new();
        private readonly Mock<IRealTimeNotifier> _realTimeNotifier = new();
        private readonly Mock<INotificationService> _notificationService = new();

        private CommentService CreateService() => new(
            _logger.Object,
            _commonRepository.Object,
            _postRepository.Object,
            _userRepository.Object,
            _commentRepository.Object,
            _realTimeNotifier.Object,
            _notificationService.Object);

        private static RegisterResponseDto BuildUserDto(Guid id, string username = "tester")
            => new(id, null, username, $"{username}@mail.com", "Test User", UserRole.Customer, new DateOnly(1990, 1, 1), null, "avatar");

        [Fact]
        public async Task PostComment_WhenBroadcastAndNotificationFail_StillReturns201()
        {
            var service = CreateService();
            var actorUserId = Guid.NewGuid();
            var postOwnerId = Guid.NewGuid();
            var post = Post.CreatePost("cap", "content", PostVisibility.Public, postOwnerId, Guid.NewGuid());
            var request = new CreateCommentDto
            {
                PostId = post.Id,
                Content = "  hello world  "
            };

            _userRepository.Setup(r => r.GetUserByIdAsync(actorUserId))
                .ReturnsAsync(BuildUserDto(actorUserId));
            _postRepository.Setup(r => r.GetPostEntityByIdAsync(post.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
            _realTimeNotifier.Setup(r => r.BroadCastComments(post.Id, It.IsAny<CommentDto>()))
                .ThrowsAsync(new InvalidOperationException("hub down"));
            _notificationService
                .Setup(s => s.CreateAndPushNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationEnum>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid?>()))
                .ThrowsAsync(new InvalidOperationException("notification down"));

            var result = await service.PostCommentAsync(request, actorUserId);

            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal("hello world", result.Data.Content);
            _commonRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _realTimeNotifier.Verify(r => r.BroadCastComments(post.Id, It.IsAny<CommentDto>()), Times.Once);
            _notificationService.Verify(s => s.CreateAndPushNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEnum>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateComment_ReturnsPopulatedDto()
        {
            var service = CreateService();
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var comment = PostComment.CreateComment(postId, userId, "old content");

            _commentRepository.Setup(r => r.GetCommentByIdAsync(comment.Id))
                .ReturnsAsync(comment);
            _userRepository.Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(BuildUserDto(userId, "updated-user"));

            var result = await service.UpdateCommentAsync(comment.Id, userId, "  updated content  ");

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal("updated content", result.Data.Content);
            Assert.Equal("updated-user", result.Data.CommentedUserName);
            Assert.Equal(comment.Id, result.Data.Id);
            Assert.Equal(comment.PostId, result.Data.PostId);
            _commonRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateComment_TooLongContent_Returns400()
        {
            var service = CreateService();
            var tooLong = new string('a', 1001);

            var result = await service.UpdateCommentAsync(Guid.NewGuid(), Guid.NewGuid(), tooLong);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
            _commentRepository.Verify(r => r.GetCommentByIdAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}

