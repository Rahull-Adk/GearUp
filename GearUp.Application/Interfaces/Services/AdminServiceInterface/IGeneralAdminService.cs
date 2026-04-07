﻿﻿using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Services.AdminServiceInterface
{
    public interface IGeneralAdminService
    {
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetAllKycs(Guid adminUserId, string? cursor);
        Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId);
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetKycsByStatus(Guid adminUserId, KycStatus status, string? cursor);
        Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string? rejectionReason = null);

        // Car methods
        Task<Result<CursorPageResult<CarResponseDto>>> GetAllCars(string? cursor);
        Task<Result<CarResponseDto>> GetCarById(Guid carId);
        Task<Result<CursorPageResult<CarResponseDto>>> GetCarsByDealerId(Guid dealerId, string? cursor);
        Task<Result<CursorPageResult<CarResponseDto>>> GetCarsByValidationStatus(CarValidationStatus status, string? cursor);
        Task<Result<string>> UpdateCarValidationStatus(Guid carId, CarValidationStatus status, Guid reviewerId, string? rejectionReason = null);
    }
}
