using FluentValidation;
using GearUp.Application.ServiceDtos.Auth;

namespace GearUp.Application.Validators
{
    public class PasswordResetValidator : AbstractValidator<PasswordResetReqDto>
    {
        public PasswordResetValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New Password is required")
                .MinimumLength(6).WithMessage("New Password must be at least 6 characters long");

            RuleFor(x => x.ConfirmedPassword)
                .NotEmpty().WithMessage("Confirmed Password is required")
                .MinimumLength(6).WithMessage("Confirmed Password must be at least 6 characters long")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }
}
