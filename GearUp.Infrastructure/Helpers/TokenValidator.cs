using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GearUp.Infrastructure.Helpers
{
    public class TokenValidator : ITokenValidator
    {
        private readonly string _audience;
        private readonly string _issuer;
        public TokenValidator(string audience, string issuer)
        {
       
            _audience = audience;
            _issuer = issuer;
        }

        public async Task<TokenValidationResultModel> ValidateToken(
    string token,
    string secretKey,
    string? expectedPurpose = null)
        {
            var key = Encoding.UTF8.GetBytes(secretKey);

            var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidAudience = _audience,
                ValidIssuer = _issuer,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            });

            if (!result.IsValid)
            {
                var errorMessage = result.Exception switch
                {
                    SecurityTokenExpiredException => "Token has expired",
                    SecurityTokenInvalidSignatureException => "Invalid token signature",
                    SecurityTokenInvalidIssuerException => "Invalid token issuer",
                    SecurityTokenInvalidAudienceException => "Invalid token audience",
                    _ => "Invalid token"
                };

                return new TokenValidationResultModel { IsValid = false, Error = errorMessage, Status = 401 };
            }

            var principal = new ClaimsPrincipal(result.ClaimsIdentity);
            if (expectedPurpose != null)
            {
                var purpose = principal.FindFirst("purpose")?.Value;
                if (purpose != expectedPurpose)
                {
                    return new TokenValidationResultModel
                    {
                        IsValid = false,
                        Error = "Token purpose mismatch",
                        Status = 401
                    };
                }
            }

            return new TokenValidationResultModel
            {
                IsValid = true,
                ClaimsPrincipal = principal,
                Error = null,
                Status = 200
            };
        }

    }
}
