using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GearUpDbContext _db;
        public AdminRepository(GearUpDbContext db)
        {
            _db = db;
        }
        public async Task<ToAdminKycListResponseDto> GetAllKycSubmissionsAsync()
        {
            var kycList = await _db.KycSubmissions
                .Select(ks => new ToAdminKycResponseDto
                {
                    Id = ks.Id,
                    UserId = ks.UserId,
                    FullName = ks.SubmittedBy!.Name,
                    Email = ks.SubmittedBy!.Email,
                    PhoneNumber = ks.SubmittedBy!.PhoneNumber,
                    DateOfBirth = ks.SubmittedBy!.DateOfBirth,
                    Status = ks.Status,
                    DocumentType = ks.DocumentType,
                    DocumentUrls = ks.DocumentUrls,
                    SelfieUrl = ks.SelfieUrl,
                    SubmittedAt = ks.SubmittedAt,
                    RejectionReason = ks.RejectionReason
                }).ToListAsync();

            var totalCount = await _db.KycSubmissions.CountAsync();

            return new ToAdminKycListResponseDto(kycList, totalCount);
        }

        public async Task<KycSubmissions?> GetKycEntityByIdAsync(Guid kycId)
        {
            return await _db.KycSubmissions.FirstOrDefaultAsync(k => k.Id == kycId);
        }

        public async Task<ToAdminKycResponseDto?> GetKycSubmissionByIdAsync(Guid kycId)
        {
            return await _db.KycSubmissions.Where(k => k.Id == kycId)
                .Select(k => new ToAdminKycResponseDto
                {
                    Id = k.Id,
                    UserId = k.UserId,
                    FullName = k.SubmittedBy!.Name,
                    Email = k.SubmittedBy!.Email,
                    PhoneNumber = k.SubmittedBy!.PhoneNumber,
                    DateOfBirth = k.SubmittedBy!.DateOfBirth,
                    Status = k.Status,
                    DocumentType = k.DocumentType,
                    DocumentUrls = k.DocumentUrls,
                    SelfieUrl = k.SelfieUrl,
                    SubmittedAt = k.SubmittedAt,
                    RejectionReason = k.RejectionReason
                }).FirstOrDefaultAsync();
        }

        public async Task<ToAdminKycListResponseDto> GetKycSubmissionsByStatusAsync(KycStatus status)
        {
            return await _db.KycSubmissions
                .Where(ks => ks.Status == status)
                .Select(ks => new ToAdminKycResponseDto
                {
                    Id = ks.Id,
                    UserId = ks.UserId,
                    FullName = ks.SubmittedBy!.Name,
                    Email = ks.SubmittedBy!.Email,
                    PhoneNumber = ks.SubmittedBy!.PhoneNumber,
                    DateOfBirth = ks.SubmittedBy!.DateOfBirth,
                    Status = ks.Status,
                    DocumentType = ks.DocumentType,
                    DocumentUrls = ks.DocumentUrls,
                    SelfieUrl = ks.SelfieUrl,
                    SubmittedAt = ks.SubmittedAt,
                    RejectionReason = ks.RejectionReason
                })
                .ToListAsync()
                .ContinueWith(t => new ToAdminKycListResponseDto(t.Result, t.Result.Count));
        }

    }
}
