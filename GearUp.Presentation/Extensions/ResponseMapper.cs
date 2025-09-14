using GearUp.Application.Common;
using GearUp.Presentation.DTOs;

namespace GearUp.Presentation.Extensions
{
    public static class ResponseMapper
    {
        public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
        {
            return new ApiResponse<T>
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "Success" : result.ErrorMessage ?? "An error occurred",
                Data = result.Data
            };
        }   
    }
}
