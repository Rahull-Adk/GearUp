using FluentValidation;
using GearUp.Application.ServiceDtos.Review;

namespace GearUp.Application.Validators
{
    public class CreateReviewRequestDtoValidator : AbstractValidator<CreateReviewRequestDto>
    {
        public CreateReviewRequestDtoValidator()
        {
            RuleFor(x => x.DealerId)
                .NotEmpty().WithMessage("Dealer ID is required.");

            RuleFor(x => x.ReviewText)
                .NotEmpty().WithMessage("Review text is required.")
                .MaximumLength(2000).WithMessage("Review text cannot exceed 2000 characters.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1.0, 5.0).WithMessage("Rating must be between 1 and 5.");
        }
    }

    public class UpdateReviewRequestDtoValidator : AbstractValidator<UpdateReviewRequestDto>
    {
        public UpdateReviewRequestDtoValidator()
        {
            RuleFor(x => x.ReviewText)
                .NotEmpty().WithMessage("Review text is required.")
                .MaximumLength(2000).WithMessage("Review text cannot exceed 2000 characters.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1.0, 5.0).WithMessage("Rating must be between 1 and 5.");
        }
    }
}
