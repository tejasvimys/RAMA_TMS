namespace RAMA_TMS.DTO
{
    public class Enable2FARequest
    {
        public string Password { get; set; } = null!;
    }

    public class Enable2FAResponse
    {
        public string Secret { get; set; } = null!;
        public string QrCodeUri { get; set; } = null!;
        public List<string> BackupCodes { get; set; } = null!;
    }

    public class Verify2FASetupRequest
    {
        public string Code { get; set; } = null!;
    }

    public class Verify2FACodeRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? TempToken { get; set; }  // Token from first auth step
    }

    public class Disable2FARequest
    {
        public string Password { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class LoginWith2FAResponse
    {
        public bool RequiresTwoFactor { get; set; }
        public string? TempToken { get; set; }
        public string? AppToken { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }
}
