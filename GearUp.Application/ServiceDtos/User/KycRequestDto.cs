using GearUp.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace GearUp.Application.ServiceDtos.User
{
    public record class KycRequestDto(KycDocumentType DocumentType, List<IFormFile> Kyc, IFormFile SelfieImage);
}
