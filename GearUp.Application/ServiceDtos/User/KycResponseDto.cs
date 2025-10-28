using GearUp.Domain.Entities;

namespace GearUp.Application.ServiceDtos.User
{
    public class KycResponseDto
    {
        public Guid Id { get; set; }
        public UserDto SubmittedBy { get; set; } = default!;
        public KycStatus Status { get; set; } = default!;
        public List<string> DocumentUrls { get; set; } = new();
        public string? SelfieUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string? AvatarUrl { get; set; }
    }

}

