
using GearUp.Infrastructure.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;


namespace GearUp.UnitTests.Infrastructure.Helpers
{
    public class TokenGeneratorTests
    {
        private readonly TokenGenerator _tokenGenerator;

        public TokenGeneratorTests()
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _tokenGenerator = new TokenGenerator(
            key,
            "Audience",
            "Issuer",
            key
            );
        }

        [Theory]
        [InlineData("access_token")]
        [InlineData("email_verification")]
        public void GeneratingTokens_ShouldReturnToken(string purpose)
        {
            var claims = new List<Claim>
            {
                new Claim("userId", "123"),
                new Claim("role", "Admin"),
                new Claim("purpose", purpose)
            };
            var tokenString = string.Empty;

            if (purpose == "access_token")
            {
                tokenString = _tokenGenerator.GenerateAccessToken(claims);
            }
            else if(purpose == "email_verification")
            {
                tokenString = _tokenGenerator.GenerateEmailVerificationToken(claims);
            }

            Assert.False(string.IsNullOrEmpty(tokenString));
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            Assert.Equal("Audience", token.Audiences.First());
            Assert.Equal("Issuer", token.Issuer);
            Assert.Contains(token.Claims, c => c.Type == "userId" && c.Value == "123");
            Assert.Contains(token.Claims, c => c.Type == "purpose" && c.Value == purpose);
        }

        [Theory]
        [InlineData("access_token", 14, 15)]
        [InlineData("email_verification", 59, 60)]
        public void GenerateTokens_ShouldExpire(string purpose, int min, int max)
        {
            var claims = new List<Claim>
            {
                new Claim("userId", "123"),
                new Claim("role", "Admin"),
                new Claim("purpose", purpose)
            };

            var tokenString = string.Empty;

            if (purpose == "access_token")
            {
                tokenString = _tokenGenerator.GenerateAccessToken(claims);
            }
            else if (purpose == "email_verification")
            {
                tokenString = _tokenGenerator.GenerateEmailVerificationToken(claims);
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            var diff = token.ValidTo - DateTime.UtcNow;
            Assert.InRange(diff.TotalMinutes, min, max);
        }

        [Theory]
        [InlineData("refresh_token")]
        [InlineData("password_reset")]
        public void GenerateingStateLessTokens_ShouldReturnRandomKey(string purpose)
        {
            string token = string.Empty;
            if(purpose == "refresh_token")
            {
                token = _tokenGenerator.GenerateRefreshToken();
            }
            else if(purpose == "password_reset")
            {
                token = _tokenGenerator.GeneratePasswordResetToken();
            }
            Assert.False(string.IsNullOrEmpty(token));
            Assert.True(Convert.FromBase64String(token).Length == 32);
        }

    }
}