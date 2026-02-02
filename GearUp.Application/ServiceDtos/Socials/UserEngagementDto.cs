namespace GearUp.Application.ServiceDtos.Socials
{
    public class UserEngagementDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}