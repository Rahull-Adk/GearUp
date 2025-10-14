using GearUp.Application.Common;


namespace GearUp.Application.Interfaces.Services.JwtServiceInterface
{
    public interface ITokenValidator
    {
        Task<TokenValidationResultModel> ValidateToken(string token, string secretKey, string? expectedPurpose = null);
    }
}
