﻿using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;

namespace GearUp.Application.Interfaces.Services.AdminServiceInterface
{
    public interface IGeneralAdminService
    {
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetAllKycs(string? cursor);
        Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId);
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetKycsByStatus(KycStatus status, string? cursor);
        Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string rejectionReason);
    }
}
