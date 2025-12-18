using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos.Auth
{
    public record RegisterResponseDto(
    Guid Id,
    string? Provider,
    string Username,
    string Email,
    string Name,
    UserRole Role, DateOnly DateOfBirth,
    string? PhoneNumber,
    string AvatarUrl
);

}
