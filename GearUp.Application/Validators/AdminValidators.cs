using FluentValidation;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;

namespace GearUp.Application.Validators
{
    public class CarValidationRequestDtoValidator : AbstractValidator<CarValidationRequestDto>
    {
        public CarValidationRequestDtoValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid validation status.");

            RuleFor(x => x.RejectionReason)
                .NotEmpty().When(x => x.Status == CarValidationStatus.Rejected)
                .WithMessage("Rejection reason is required when rejecting a car.");
        }
    }

    public class KycReviewRequestDtoValidator : AbstractValidator<kycRequestDto>
    {
        public KycReviewRequestDtoValidator()
        {
             RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid validation status.");

            RuleFor(x => x.RejectionReason)
                .NotEmpty().When(x => x.Status == KycStatus.Rejected)
                .WithMessage("Rejection reason is required when rejecting KYC.");
        }
    }
}
