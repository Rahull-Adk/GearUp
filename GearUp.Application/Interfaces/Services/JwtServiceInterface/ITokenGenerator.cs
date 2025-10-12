using System.Security.Claims;

namespace GearUp.Application.Interfaces.Services.JwtServiceInterface
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateEmailVerificationToken(IEnumerable<Claim> claims);
    }
}
