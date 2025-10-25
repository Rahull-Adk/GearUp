namespace GearUp.Application.ServiceDtos.User
{
        public record UpdateUserResponseDto(
            Guid Id,
            string Username,
            string Email,
            string? PendingEmail,
            string Name,
            string? AvatarUrl,
            bool IsEmailVerified,
            bool IsPendingEmailVerified,
            DateTime UpdatedAt
        );
    }
