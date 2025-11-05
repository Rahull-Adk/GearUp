using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GearUp.Infrastructure.Helpers
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly string _accessToken_SecretKey;
        private readonly string _audience;
        private readonly string _issuer;
        private readonly string _emailVerificationToken_SecretKey;
        public TokenGenerator(string accessToken_SecretKey, string audience, string issuer, string emailVerificationToken_SecretKey)
        {
            _accessToken_SecretKey = accessToken_SecretKey;
            _audience = audience;
            _issuer = issuer;
            _emailVerificationToken_SecretKey = emailVerificationToken_SecretKey;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var secretKey = _accessToken_SecretKey;
            var token = GenerateToken(claims, 15, secretKey);
            return token;
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));  
        }

        public string GenerateEmailVerificationToken(IEnumerable<Claim> claims)
        {
            var token = GenerateToken(claims, 60, _emailVerificationToken_SecretKey);
            return token;
        }

        public string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));  
        }

        private string GenerateToken(IEnumerable<Claim> claims, int timeInMin, string secretKey)
        {
            var token = new JwtSecurityToken(
               issuer: _issuer,
               audience: _audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(timeInMin),
               signingCredentials: new SigningCredentials(
                   new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
                   SecurityAlgorithms.HmacSha256)
           );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
       
    }
}
