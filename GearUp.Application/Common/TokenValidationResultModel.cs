using System.Security.Claims;

namespace GearUp.Application.Common
{
    public class TokenValidationResultModel
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public ClaimsPrincipal? ClaimsPrincipal { get; set; }
        public int Status { get; set; }
        
    }
}
