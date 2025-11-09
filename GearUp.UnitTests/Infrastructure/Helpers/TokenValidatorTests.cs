using GearUp.Infrastructure.Helpers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GearUp.UnitTests.Infrastructure.Helpers
{
    public class TokenValidatorTests
    {
        private readonly TokenValidator _tokenValidator;
        private readonly TokenGenerator _tokenGenerator;
        private readonly string _key;
        public TokenValidatorTests()
        {
            _key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _tokenValidator = new TokenValidator("Audience", "Issuer");
            _tokenGenerator = new TokenGenerator(
                _key,
                "Audience",
                "Issuer",
                _key
            );
        }
        [Theory]
        [InlineData("access_token")]
        [InlineData("email_verification")]
        public async Task ValidatingTokens_ShouldReturn_TokenValidationResultModel(string purpose)
        {
            var claims = new List<Claim>
            {
                new Claim("purpose", purpose),
                new Claim("id", "123"),
                new Claim("email", "user@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
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

            var result = await _tokenValidator.ValidateToken(tokenString,
                _key,
                purpose);
            Assert.True(result.IsValid);
            Assert.Null(result.Error);
            Assert.NotNull(result.ClaimsPrincipal);
            Assert.Equal(purpose, result.ClaimsPrincipal!.FindFirst("purpose")?.Value);
        }




    }
}
