using FluentValidation;
using GearUp.Application.ServiceDtos.Appointment;

namespace GearUp.Application.Validators
{
    public class CreateAppointmentRequestDtoValidator : AbstractValidator<CreateAppointmentRequestDto>
    {
        public CreateAppointmentRequestDtoValidator()
        {
            RuleFor(x => x.AgentId)
                .NotEmpty().WithMessage("Agent ID is required.");

            RuleFor(x => x.Schedule)
                .NotEmpty().WithMessage("Schedule is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Schedule must be in the future.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.");
        }
    }

    public class UpdateAppointmentStatusDtoValidator : AbstractValidator<UpdateAppointmentStatusDto>
    {
        public UpdateAppointmentStatusDtoValidator()
        {
            // RejectionReason is optional depending on the status transition, 
            // but we can add conditional logic if needed. 
            // For now, keep it simple or check if it's required for rejection.
        }
    }
}
