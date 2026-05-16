using FluentValidation;
using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Validators
{
    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.PostId)
                .NotEmpty().WithMessage("Post ID is required.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(1000).WithMessage("Comment content cannot exceed 1000 characters.");
        }
    }

    public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
    {
        public UpdateCommentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(1000).WithMessage("Comment content cannot exceed 1000 characters.");
        }
    }

    public class UpdatePostDtoValidator : AbstractValidator<UpdatePostDto>
    {
        public UpdatePostDtoValidator()
        {
            RuleFor(x => x.Caption)
                .NotEmpty().WithMessage("Caption is required.")
                .MaximumLength(500).WithMessage("Caption cannot exceed 500 characters.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.");

            RuleFor(x => x.Visibility)
                .IsInEnum().WithMessage("Invalid visibility status.");
        }
    }
}
