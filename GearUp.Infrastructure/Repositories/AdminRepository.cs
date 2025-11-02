using GearUp.Application.Interfaces.Repositories;
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
        public async Task<ICollection<KycSubmissions>> GetAllKycSubmissionsAsync()
        {
            return await _db.KycSubmissions.Include(k => k.SubmittedBy).ToListAsync();
        }

        public async Task<KycSubmissions?> GetKycSubmissionByIdAsync(Guid kycId)
        {
            return await _db.KycSubmissions.Include(k => k.SubmittedBy)
                                           .FirstOrDefaultAsync(k => k.Id == kycId);
        }

        public async Task<ICollection<KycSubmissions>> GetKycSubmissionsByStatusAsync(KycStatus status)
        {
            return await _db.KycSubmissions
                            .Include(k => k.SubmittedBy)
                            .Where(k => k.Status == status)
                            .ToListAsync();
        }

    }
}
