namespace GearUp.Application.ServiceDtos.Auth
{
    public record RegisterResponseDto(
    Guid Id,
    string? Provider,
    string Username,
    string Email,
    string Name,
    string Role, DateOnly DateOfBirth,
    string? PhoneNumber,
    string AvatarUrl
);

}
