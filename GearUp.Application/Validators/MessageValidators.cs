using FluentValidation;
using GearUp.Application.ServiceDtos.Message;

namespace GearUp.Application.Validators
{
    public class SendMessageRequestDtoValidator : AbstractValidator<SendMessageRequestDto>
    {
        public SendMessageRequestDtoValidator()
        {
            RuleFor(x => x.ReceiverId)
                .NotEmpty().WithMessage("Receiver ID is required.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Text) || !string.IsNullOrWhiteSpace(x.ImageUrl))
                .WithMessage("Message must contain either text or an image.");

            RuleFor(x => x.Text)
                .MaximumLength(2000).WithMessage("Message text cannot exceed 2000 characters.");
        }
    }
}
