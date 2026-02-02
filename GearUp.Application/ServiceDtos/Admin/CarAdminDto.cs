using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos.Admin
{
    public record CarValidationRequestDto
    {
        public CarValidationStatus Status { get; init; }
        public string? RejectionReason { get; init; } = null;
    }
}
