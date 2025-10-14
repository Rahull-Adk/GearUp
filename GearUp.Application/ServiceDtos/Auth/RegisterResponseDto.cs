namespace GearUp.Application.ServiceDtos.Auth
{
    public class RegisterResponseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; }
        public string AvartarUrl { get; set; } = string.Empty;
    }
}
