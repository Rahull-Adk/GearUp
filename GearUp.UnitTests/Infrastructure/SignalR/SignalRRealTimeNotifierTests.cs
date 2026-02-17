using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace GearUp.UnitTests.Infrastructure.SignalR
{
    public class SignalRRealTimeNotifierTests
    {
        private readonly Mock<IHubContext<PostHub>> _postHubMock;
        private readonly Mock<IHubContext<NotificationHub>> _notificationHubMock;
        private readonly Mock<IHubContext<ChatHub>> _chatHubMock;
        private readonly Mock<IHubClients> _clientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly SignalRRealTimeNotifier _notifier;

        public SignalRRealTimeNotifierTests()
        {
            _postHubMock = new Mock<IHubContext<PostHub>>();
            _notificationHubMock = new Mock<IHubContext<NotificationHub>>();
            _chatHubMock = new Mock<IHubContext<ChatHub>>();
            _clientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _postHubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
            _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _notifier = new SignalRRealTimeNotifier(_postHubMock.Object, _notificationHubMock.Object, _chatHubMock.Object);
        }

        [Fact]
        public async Task BroadCastComments_ShouldUseCorrectGroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var comment = new CommentDto
            {
                Id = Guid.NewGuid(),
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow
            };
            var expectedGroupName = $"post-{postId}-comments";

            // Act
            await _notifier.BroadCastComments(postId, comment);

            // Assert
            _clientsMock.Verify(c => c.Group(expectedGroupName), Times.Once);
            _clientProxyMock.Verify(
                c => c.SendCoreAsync("CommentCreated", It.Is<object[]>(o => o[0] == comment), default),
                Times.Once
            );
        }

        [Fact]
        public async Task BroadCastCommentLikes_ShouldUseCorrectGroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var likeCount = 5;
            var expectedGroupName = $"post-{postId}-comments";

            // Act
            await _notifier.BroadCastCommentLikes(postId, commentId, likeCount);

            // Assert
            _clientsMock.Verify(c => c.Group(expectedGroupName), Times.Once);
            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "CommentLikeUpdated",
                    It.Is<object[]>(o =>
                        o.Length == 1 &&
                        o[0] != null
                    ),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task BroadCastPostLikes_ShouldUseCorrectGroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var likeCount = 10;
            var expectedGroupName = $"post-{postId}";

            // Act
            await _notifier.BroadCastPostLikes(postId, likeCount);

            // Assert
            _clientsMock.Verify(c => c.Group(expectedGroupName), Times.Once);
            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "PostLikeUpdated",
                    It.Is<object[]>(o =>
                        o.Length == 1 &&
                        o[0] != null
                    ),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task BroadCastComments_ShouldSendCorrectEventName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var comment = new CommentDto { Id = Guid.NewGuid(), Content = "Test", CreatedAt = DateTime.UtcNow };

            // Act
            await _notifier.BroadCastComments(postId, comment);

            // Assert
            _clientProxyMock.Verify(
                c => c.SendCoreAsync("CommentCreated", It.IsAny<object[]>(), default),
                Times.Once
            );
        }

        [Fact]
        public async Task BroadCastCommentLikes_ShouldSendCorrectEventName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var likeCount = 5;

            // Act
            await _notifier.BroadCastCommentLikes(postId, commentId, likeCount);

            // Assert
            _clientProxyMock.Verify(
                c => c.SendCoreAsync("CommentLikeUpdated", It.IsAny<object[]>(), default),
                Times.Once
            );
        }

        [Fact]
        public async Task BroadCastPostLikes_ShouldSendCorrectEventName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var likeCount = 10;

            // Act
            await _notifier.BroadCastPostLikes(postId, likeCount);

            // Assert
            _clientProxyMock.Verify(
                c => c.SendCoreAsync("PostLikeUpdated", It.IsAny<object[]>(), default),
                Times.Once
            );
        }
    }
}
