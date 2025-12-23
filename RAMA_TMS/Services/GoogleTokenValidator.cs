using Google.Apis.Auth;
using RAMA_TMS.Interface;

namespace RAMA_TMS.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly string _clientId;

        public GoogleTokenValidator(IConfiguration config)
        {
            _clientId = config["GoogleAuth:ClientId"]
                        ?? throw new InvalidOperationException("GoogleAuth:ClientId not configured");
        }

        public async Task<string?> ValidateAndGetEmailAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return payload.Email;
            }
            catch
            {
                return null;
            }
        }
    }
}
