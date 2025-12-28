namespace GearUp.Application.Interfaces
{
    public interface IRealTimeNotifier
    {
        Task BroadCastCommentToPostViewers(Guid postId);
        Task BroadCastCommentLikesToPostViewers(Guid postId);
        Task BroadCastLikesToPostViewers(Guid postId);
    }
}
