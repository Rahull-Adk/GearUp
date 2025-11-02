
using FluentValidation;
using GearUp.Application.ServiceDtos.Auth;

namespace GearUp.Application.Validators
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {

            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("Username or Email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
        }
    }

    public class AdminLoginRequestDtoValidator : AbstractValidator<AdminLoginRequestDto>
    {
        public AdminLoginRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
        }
    }
}