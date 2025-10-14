using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

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
            var secretKey = Environment.GetEnvironmentVariable("AccessToken_SecretKey")!;
            var token = GenerateToken(claims, 15, secretKey);
            return token;
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));  
        }

        public string GenerateEmailVerificationToken(IEnumerable<Claim> claims)
        {

            var token = GenerateToken(claims, 1440, Environment.GetEnvironmentVariable("EmailVerificationToken_SecretKey")!);
            return token;
        }

        private string GenerateToken(IEnumerable<Claim> claims, int timeInMin, string secretKey)
        {
            var token = new JwtSecurityToken(
               issuer: _jwtSetting.Value.Issuer,
               audience: _jwtSetting.Value.Audience,
               claims: claims,
               expires: DateTime.Now.AddMinutes(timeInMin),
               signingCredentials: new SigningCredentials(
                   new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
                   SecurityAlgorithms.HmacSha256)
           );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

       
    }
}
