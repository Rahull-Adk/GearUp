using FluentValidation;
using GearUp.Application.ServiceDtos.User;

namespace GearUp.Application.Validators
{
    public class UpdateReqDtoValidator : AbstractValidator<UpdateUserRequestDto>
    {
        public UpdateReqDtoValidator()
        {
            RuleFor(x => x.NewEmail)
                 .EmailAddress().WithMessage("New email must be a valid email address.")
                 .When(x => !string.IsNullOrEmpty(x.NewEmail));

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number is not valid.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("Date of birth must be in the past.");

            RuleFor(x => x.AvatarImage)
                .Must(file => file == null || file.Length <= 5 * 1024 * 1024).WithMessage("Avatar image size must be less than or equal to 5MB.")
                .When(x => x.AvatarImage != null);

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required to change password.")
                .MinimumLength(6).WithMessage("Current password must be at least 6 characters long.")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword) || !string.IsNullOrWhiteSpace(x.ConfirmedNewPassword));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(6).WithMessage("New password must be at least 6 characters long.")
                .Equal(x => x.ConfirmedNewPassword).WithMessage("New password and confirmation password do not match.")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrentPassword));

            RuleFor(x => x.ConfirmedNewPassword)
                .NotEmpty().WithMessage("Confirmation password is required.")
                .Equal(x => x.NewPassword).WithMessage("New password and confirmation password do not match.")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));

        }
    }
}
