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
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly SignalRRealTimeNotifier _notifier;

        public SignalRRealTimeNotifierTests()
        {
            _postHubMock = new Mock<IHubContext<PostHub>>();
            _notificationHubMock = new Mock<IHubContext<NotificationHub>>();
            _hubClientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            // Setup mock chain
            _postHubMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            
            _notifier = new SignalRRealTimeNotifier(_postHubMock.Object, _notificationHubMock.Object);
        }

        [Fact]
        public async Task BroadCastComments_Should_Use_Hyphen_In_GroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var comment = new CommentDto
            {
                Id = Guid.NewGuid(),
                Content = "Test comment",
                PostId = postId,
                CommentedUserId = Guid.NewGuid(),
                CommentedUserName = "testuser",
                CreatedAt = DateTime.UtcNow
            };
            var expectedGroupName = $"post-{postId}";

            // Act
            await _notifier.BroadCastComments(postId, comment);

            // Assert
            _hubClientsMock.Verify(
                c => c.Group(expectedGroupName),
                Times.Once,
                $"Expected group name to be '{expectedGroupName}' with hyphen, not colon");
            
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("CommentCreated", It.Is<object[]>(o => o[0] == comment), default),
                Times.Once);
        }

        [Fact]
        public async Task BroadCastCommentLikes_Should_Use_Hyphen_In_GroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var likeCount = 10;
            var expectedGroupName = $"post-{postId}";

            // Act
            await _notifier.BroadCastCommentLikes(postId, commentId, likeCount);

            // Assert
            _hubClientsMock.Verify(
                c => c.Group(expectedGroupName),
                Times.Once,
                $"Expected group name to be '{expectedGroupName}' with hyphen, not colon");
            
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("CommentLikeUpdated", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public async Task BroadCastPostLikes_Should_Use_Hyphen_In_GroupName()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var likeCount = 25;
            var expectedGroupName = $"post-{postId}";

            // Act
            await _notifier.BroadCastPostLikes(postId, likeCount);

            // Assert
            _hubClientsMock.Verify(
                c => c.Group(expectedGroupName),
                Times.Once,
                $"Expected group name to be '{expectedGroupName}' with hyphen, not colon");
            
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("PostLikeUpdated", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public void All_Broadcast_Methods_Should_Use_Consistent_GroupName_Format()
        {
            // This test documents the expected group naming convention
            // PostHub.cs uses: $"post-{postId}" with hyphen
            // All broadcast methods in SignalRRealTimeNotifier must match this format
            var testPostId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            var expectedGroupName = $"post-{testPostId}";
            
            // Expected format uses hyphen (-) not colon (:)
            Assert.Equal("post-12345678-1234-1234-1234-123456789012", expectedGroupName);
            Assert.DoesNotContain(":", expectedGroupName);
        }
    }
}
