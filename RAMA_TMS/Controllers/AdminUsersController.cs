using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Helpers;
using RAMA_TMS.Models.Auth;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly TMSDBContext _context;

        public AdminUsersController(TMSDBContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUserListItemDto>>> GetUsers()
        {
            var users = await _context.AppUsers
                .OrderBy(u => u.Email)
                .Select(u => new AppUserListItemDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/admin/users/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateAppUserDto dto)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound();

            // Prevent demoting yourself from Admin to something else if desired
            // (optional safety)
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/admin/users/{id}/2fa/status
        [HttpGet("{id:long}/2fa/status")]
        public async Task<ActionResult> GetUser2FAStatus(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                twoFactorEnabled = user.TwoFactorEnabled,
                backupCodesCount = user.BackupCodes?.Count ?? 0
            });
        }

        // POST: api/admin/users/{id}/2fa/enable
        [HttpPost("{id:long}/2fa/enable")]
        public async Task<ActionResult<Enable2FAResponse>> EnableUser2FA(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            if (user.TwoFactorEnabled)
                return BadRequest("2FA is already enabled for this user.");

            // Generate secret and backup codes
            var secret = TwoFactorHelper.GenerateSecret();
            var backupCodes = TwoFactorHelper.GenerateBackupCodes(10);
            var qrCodeUri = TwoFactorHelper.GetProvisioningUri(user.Email, secret);

            // Enable 2FA for the user
            user.TwoFactorSecret = secret;
            user.BackupCodes = backupCodes;
            user.TwoFactorEnabled = true;

            await _context.SaveChangesAsync();

            return new Enable2FAResponse
            {
                Secret = secret,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes
            };
        }

        // POST: api/admin/users/{id}/2fa/disable
        [HttpPost("{id:long}/2fa/disable")]
        public async Task<ActionResult> DisableUser2FA(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            if (!user.TwoFactorEnabled)
                return BadRequest("2FA is not enabled for this user.");

            // Disable 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.BackupCodes = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "2FA disabled successfully for user." });
        }

        // POST: api/admin/users/{id}/2fa/reset
        [HttpPost("{id:long}/2fa/reset")]
        public async Task<ActionResult<Enable2FAResponse>> ResetUser2FA(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            // Generate new secret and backup codes
            var secret = TwoFactorHelper.GenerateSecret();
            var backupCodes = TwoFactorHelper.GenerateBackupCodes(10);
            var qrCodeUri = TwoFactorHelper.GetProvisioningUri(user.Email, secret);

            // Reset 2FA for the user
            user.TwoFactorSecret = secret;
            user.BackupCodes = backupCodes;
            user.TwoFactorEnabled = true;

            await _context.SaveChangesAsync();

            return new Enable2FAResponse
            {
                Secret = secret,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes
            };
        }

        // DELETE: api/admin/users/{id} - Soft delete (deactivate)
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            // Prevent deactivating yourself
            var currentUserIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == System.Security.Claims.ClaimTypes.NameIdentifier ||
                c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (currentUserIdClaim != null && long.TryParse(currentUserIdClaim.Value, out long currentUserId))
            {
                if (currentUserId == id)
                    return BadRequest("You cannot deactivate your own account.");
            }

            // Soft delete - just deactivate the user
            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deactivated successfully." });
        }

    }
}
