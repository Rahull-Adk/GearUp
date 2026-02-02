﻿using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Services.AdminServiceInterface
{
    public interface IGeneralAdminService
    {
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetAllKycs(string? cursor);
        Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId);
        Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetKycsByStatus(KycStatus status, string? cursor);
        Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string rejectionReason);

        // Car methods
        Task<Result<PageResult<CarResponseDto>>> GetAllCars(int pageNum, int pageSize = 10);
        Task<Result<CarResponseDto>> GetCarById(Guid carId);
        Task<Result<PageResult<CarResponseDto>>> GetCarsByDealerId(Guid dealerId, int pageNum, int pageSize = 10);
        Task<Result<PageResult<CarResponseDto>>> GetCarsByValidationStatus(CarValidationStatus status, int pageNum, int pageSize = 10);
        Task<Result<string>> UpdateCarValidationStatus(Guid carId, CarValidationStatus status, Guid reviewerId, string? rejectionReason = null);
    }
}
