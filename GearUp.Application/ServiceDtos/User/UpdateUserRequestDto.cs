namespace GearUp.Application.ServiceDtos.User
{
    public record class UpdateUserRequestDto(string? NewEmail, string? Name, string? AvatarUrl, DateOnly? DateOfBirth, string? PhoneNumber, string? CurrentPassword, string? NewPassword, string ConfirmedNewPassword);
}
