using Microsoft.AspNetCore.Http;

namespace GearUp.Application.ServiceDtos.User
{
    public record class UpdateUserRequestDto(string? NewEmail, string? Name, IFormFile? AvatarImage, DateOnly? DateOfBirth, string? PhoneNumber, string? CurrentPassword, string? NewPassword, string? ConfirmedNewPassword);
}
