using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Helpers;
using System.Security.Cryptography;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/password-reset")]
    public class PasswordResetController : ControllerBase
    {
        private readonly TMSDBContext _context;
        private readonly IConfiguration _config;

        public PasswordResetController(TMSDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            var email = request.Email.Trim().ToLower();

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // Always return success to prevent email enumeration
            if (user == null)
            {
                return Ok(new ForgotPasswordResponse
                {
                    Message = "If an account exists with this email, a password reset link has been sent."
                });
            }

            // Generate secure reset token
            var token = GenerateSecureToken();
            var expiry = DateTimeOffset.UtcNow.AddHours(1); // Token valid for 1 hour

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = expiry;

            await _context.SaveChangesAsync();

            // TODO: Send email with reset link
            // For now, we'll return the token in development mode
            var isDevelopment = _config.GetValue<bool>("IsDevelopment", false);

            if (isDevelopment)
            {
                return Ok(new ForgotPasswordResponse
                {
                    Message = "Password reset token generated (development mode).",
                    ResetToken = token
                });
            }

            // In production, send email and don't return token
            // await SendPasswordResetEmail(user.Email, token);

            return Ok(new ForgotPasswordResponse
            {
                Message = "If an account exists with this email, a password reset link has been sent."
            });
        }

        [HttpPost("validate-token")]
        public async Task<ActionResult> ValidateResetToken([FromBody] ValidateResetTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token is required.");

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

            if (user == null)
                return BadRequest("Invalid reset token.");

            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
                return BadRequest("Reset token has expired.");

            return Ok(new { message = "Token is valid.", email = user.Email });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest("Password must be at least 6 characters long.");

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

            if (user == null)
                return BadRequest("Invalid reset token.");

            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
                return BadRequest("Reset token has expired.");

            // Hash new password
            var (hash, salt) = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            // Clear reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }

        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        // TODO: Implement email sending
        // private async Task SendPasswordResetEmail(string email, string token)
        // {
        //     var resetLink = $"https://yourdomain.com/reset-password?token={token}";
        //     // Send email using your email service
        // }

    }
}
