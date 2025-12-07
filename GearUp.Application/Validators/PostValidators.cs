using FluentValidation;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Validators
{
    public class PostValidators : AbstractValidator<CreatePostRequestDto>
    {
        public PostValidators()
        {
            RuleFor(x => x.Caption)
                .NotEmpty().WithMessage("Caption is required")
                .MaximumLength(500).WithMessage("Caption cannot exceed 500 characters");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required");

            RuleFor(x => x.CarId)
                .Must(id => id != Guid.Empty).WithMessage("CarId must be a valid GUID");

            RuleFor(x => x.Visibility)
               .NotEqual(PostVisibility.Default).WithMessage("Visibility must be set to a valid option")
               .IsInEnum().WithMessage("Invalid visibility option");


        }
    }
}
