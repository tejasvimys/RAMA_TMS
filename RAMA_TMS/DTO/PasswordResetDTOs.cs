namespace RAMA_TMS.DTO
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = null!;
        public string? ResetToken { get; set; } // For development/testing only
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class ValidateResetTokenRequest
    {
        public string Token { get; set; } = null!;
    }
}
