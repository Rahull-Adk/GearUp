using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Interfaces
{
    public interface IRealTimeNotifier
    {
        Task BroadCastCommentToPostViewers(Guid postId, CommentDto comment);
        Task BroadCastCommentLikesToPostViewers(Guid postId, int likeCountOnComment);
        Task BroadCastLikesToPostViewers(Guid postId, int likeCountOnPost);
    }
}
