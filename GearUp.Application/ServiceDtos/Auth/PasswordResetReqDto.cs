
namespace GearUp.Application.ServiceDtos.Auth
{
    public class PasswordResetReqDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmedPassword { get; set; } = string.Empty;

    }
}
