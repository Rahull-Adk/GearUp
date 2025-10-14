
using FluentValidation;
using GearUp.Application.ServiceDtos.Auth;
using System.Net.Mail;

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
}
