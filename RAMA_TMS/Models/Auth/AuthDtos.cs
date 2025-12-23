namespace RAMA_TMS.Models.Auth
{
    public class TokenExchangeRequest
    {
        public string Provider { get; set; } = "google"; // "google", "auth0", etc. or set to null to pass default from config
        public string IdToken { get; set; } = null!;
    }

    public class TokenExchangeResponse
    {
        public string AppToken { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }      // true = can use app
        public bool IsNewUser { get; set; }     // true = just registered, pending approval
    }
}
