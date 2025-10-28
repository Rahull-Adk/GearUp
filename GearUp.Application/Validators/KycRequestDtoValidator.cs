using FluentValidation;
using GearUp.Application.ServiceDtos.User;

namespace GearUp.Application.Validators
{
    public class KycRequestDtoValidator : AbstractValidator<KycRequestDto>
    {
        public KycRequestDtoValidator()
        {
            RuleFor(x => x.DocumentType)
                .IsInEnum().WithMessage("Invalid document type");

            RuleFor(x => x.Kyc)
                .Must(files => files != null && files.Count > 0).WithMessage("At least one KYC document must be provided")
                .Must(files => files.All(file => file.Length < 5 * 1024 * 1024)).WithMessage("Each KYC document must be less than 5MB in size")
                .NotEmpty().WithMessage("KYC documents are required");

            RuleFor(x => x.SelfieImage)
                .Must(file => file != null).WithMessage("Selfie image is required")
                .Must(file => file.Length < 5 * 1024 * 1024).WithMessage("Selfie image must be less than 5MB in size")
                .NotEmpty().WithMessage("Selfie image is required");


        }
    }
}
