namespace GearUp.Application.Interfaces
{
    public interface IRealTimeNotifier
    {
        Task BroadCastLikeToPostViewers(Guid postId, int likeCount);
        Task BroadCastCommentToPostViewers(Guid postId, string comment);
        Task BroadCastLikesToCommentViewers(Guid commentId, int likeCount);
    }
}
