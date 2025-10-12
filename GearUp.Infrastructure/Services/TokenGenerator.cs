using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sprache;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GearUp.Infrastructure.JwtServices
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly IOptions<JwtSetting> _jwtSetting;
        public TokenGenerator(IOptions<JwtSetting> jwtSetting)
        {
            _jwtSetting = jwtSetting;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                issuer: _jwtSetting.Value.Issuer,
                audience: _jwtSetting.Value.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSetting.Value.AcessToken_SecretKey)),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateEmailVerificationToken(IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                issuer: _jwtSetting.Value.Issuer,
                audience: _jwtSetting.Value.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSetting.Value.EmailVerificationToken_SecretKey)),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
