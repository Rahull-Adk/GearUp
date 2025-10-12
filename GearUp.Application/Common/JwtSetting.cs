namespace GearUp.Application.Common
{
    public class JwtSetting
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string AcessToken_SecretKey{ get; set; }
        public string RefreshToken_SecretKey{ get; set; }
        public string EmailVerificationToken_SecretKey { get; set; }
    }
}
