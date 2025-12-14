
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;

namespace GearUp.Infrastructure.Helpers
{
    public class BrevoEmailClient : ITransactionalEmailClient
    {
        private readonly TransactionalEmailsApi _api;
        public BrevoEmailClient(string apiKey)
        {
            sib_api_v3_sdk.Client.Configuration.Default.ApiKey["api-key"] = apiKey;
            _api = new TransactionalEmailsApi();
        }
        public async Task<CreateSmtpEmail> SendAsync(SendSmtpEmail email)
        {
            return await  _api.SendTransacEmailAsync(email);
        }
    }
}
