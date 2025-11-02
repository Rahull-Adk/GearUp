using GearUp.Domain.Entities;

namespace GearUp.Application.ServiceDtos.Admin
{
    public record class ToAdminKycResponseDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public DateOnly? DateOfBirth { get; init; }
        public KycStatus Status { get; init; }
        public KycDocumentType DocumentType { get; init; }
        public List<Uri> DocumentUrls { get; init; } = new();
        public string SelfieUrl { get; init; } = string.Empty;
        public DateTime SubmittedAt { get; init; }
        public string? RejectionReason { get; init; }
    }

    public record class kycRequestDto
    {
        public KycStatus Status { get; init; }
        public string? RejectionReason { get; init; } = null;
    }


    public record class ToAdminKycListResponseDto(List<ToAdminKycResponseDto> KycSubmissions, int TotalCount);
}
