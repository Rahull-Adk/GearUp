using GearUp.Application.Common;
using GearUp.Presentation.DTOs;

namespace GearUp.Presentation.Extensions
{
    public static class ResponseMapper
    {
        public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
        {
            string message = string.IsNullOrEmpty(result.SuccessMessage) ? result.ErrorMessage : result.SuccessMessage;
            return new ApiResponse<T>
            {
                IsSuccess = result.IsSuccess,
                Message = message,
                Data = result.Data,
                Status = result.Status
            };
        }   
    }
}
