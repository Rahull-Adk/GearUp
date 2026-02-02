using GearUp.Application.Common.Pagination;
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
        private const int PageSize = 10;

        public AdminRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task<CursorPageResult<ToAdminKycResponseDto>> GetAllKycSubmissionsAsync(Cursor? cursor)
        {
            IQueryable<KycSubmissions> query = _db.KycSubmissions
                .AsNoTracking()
                .OrderByDescending(ks => ks.SubmittedAt)
                .ThenByDescending(ks => ks.Id);

            if (cursor is not null)
            {
                query = query.Where(ks => ks.SubmittedAt < cursor.CreatedAt ||
                    (ks.SubmittedAt == cursor.CreatedAt && ks.Id.CompareTo(cursor.Id) < 0));
            }

            var kycList = await query
                .Take(PageSize + 1)
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

            bool hasMore = kycList.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = kycList[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.SubmittedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<ToAdminKycResponseDto>
            {
                Items = kycList.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
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

        public async Task<CursorPageResult<ToAdminKycResponseDto>> GetKycSubmissionsByStatusAsync(KycStatus status, Cursor? cursor)
        {
            IQueryable<KycSubmissions> query = _db.KycSubmissions
                .AsNoTracking()
                .Where(ks => ks.Status == status)
                .OrderByDescending(ks => ks.SubmittedAt)
                .ThenByDescending(ks => ks.Id);

            if (cursor is not null)
            {
                query = query.Where(ks => ks.SubmittedAt < cursor.CreatedAt ||
                    (ks.SubmittedAt == cursor.CreatedAt && ks.Id.CompareTo(cursor.Id) < 0));
            }

            var kycList = await query
                .Take(PageSize + 1)
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
                .ToListAsync();

            bool hasMore = kycList.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = kycList[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.SubmittedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<ToAdminKycResponseDto>
            {
                Items = kycList.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }
    }
}
