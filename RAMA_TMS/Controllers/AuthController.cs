using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Helpers;
using RAMA_TMS.Interface;
using RAMA_TMS.Models.Auth;
using RAMA_TMS.Models.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TMSDBContext _context;
        private readonly IConfiguration _config;
        public AuthController(TMSDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var email = request.Email.Trim().ToLower();

            var existing = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (existing != null)
                return BadRequest("User with this email already exists.");

            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            var user = new AppUser
            {
                Email = email,
                DisplayName = request.DisplayName.Trim(),
                Role = "Collector",
                IsActive = false,
                PasswordHash = hash,
                PasswordSalt = salt,
                TwoFactorEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "self-registration"
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registered. Waiting for admin approval." });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginWith2FAResponse>> Login([FromBody] LoginRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (appUser == null || string.IsNullOrEmpty(appUser.PasswordHash) || string.IsNullOrEmpty(appUser.PasswordSalt))
                return Unauthorized("Invalid credentials.");

            if (!PasswordHasher.Verify(request.Password, appUser.PasswordHash, appUser.PasswordSalt))
                return Unauthorized("Invalid credentials.");

            if (!appUser.IsActive)
            {
                return new LoginWith2FAResponse
                {
                    RequiresTwoFactor = false,
                    Requires2FASetup = false,
                    AppToken = null,
                    Email = appUser.Email,
                    DisplayName = appUser.DisplayName,
                    Role = appUser.Role,
                    IsActive = false
                };
            }

            // NEW: Check if user needs to setup 2FA for the first time
            if (!appUser.TwoFactorEnabled)
            {
                // Generate temporary token for 2FA setup
                var tempToken = GenerateTempToken(appUser.Id, appUser.Email);

                return new LoginWith2FAResponse
                {
                    RequiresTwoFactor = false,
                    Requires2FASetup = true,
                    TempToken = tempToken,
                    Email = appUser.Email,
                    DisplayName = appUser.DisplayName,
                    Role = appUser.Role,
                    IsActive = true
                };
            }

            // Check if 2FA is enabled (normal flow)
            if (appUser.TwoFactorEnabled)
            {
                // Generate temporary token for 2FA verification
                var tempToken = GenerateTempToken(appUser.Id, appUser.Email);

                return new LoginWith2FAResponse
                {
                    RequiresTwoFactor = true,
                    Requires2FASetup = false,
                    TempToken = tempToken,
                    Email = appUser.Email,
                    IsActive = true
                };
            }

            // This should never happen if 2FA is enforced
            return Unauthorized("2FA is required for all users.");
        }

        [HttpPost("verify-2fa")]
        public async Task<ActionResult<TokenExchangeResponse>> Verify2FA([FromBody] Verify2FACodeRequest request)
        {
            if (string.IsNullOrEmpty(request.TempToken))
                return BadRequest("Temporary token is required.");

            // Validate temp token
            var userId = ValidateTempToken(request.TempToken);
            if (userId == null)
                return Unauthorized("Invalid or expired token. Please login again.");

            var appUser = await _context.AppUsers.FindAsync(userId.Value);
            var user2fa = await _context.AppUsers
        .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (appUser == null || !appUser.TwoFactorEnabled)
                return Unauthorized("Invalid request.");

            // Verify 2FA code
            bool isValid = false;

            // Check if it's a TOTP code
            if (!string.IsNullOrEmpty(appUser.TwoFactorSecret))
            {
                isValid = TwoFactorHelper.ValidateCode(appUser.TwoFactorSecret, request.Code);
            }

            // If TOTP failed, check backup codes
            if (!isValid && appUser.BackupCodes != null && appUser.BackupCodes.Count > 0)
            {
                if (appUser.BackupCodes.Contains(request.Code))
                {
                    isValid = true;
                    // Remove used backup code
                    appUser.BackupCodes.Remove(request.Code);
                    await _context.SaveChangesAsync();
                }
            }

            if (!isValid)
                return Unauthorized("Invalid 2FA code.");

            // Issue full JWT token
            var token = GenerateJwtToken(appUser);

            return new TokenExchangeResponse
            {
                AppToken = token,
                Email = appUser.Email,
                DisplayName = appUser.DisplayName,
                Role = appUser.Role,
                IsActive = true,
                IsNewUser = false,
                TwoFactorSecret = user2fa.TwoFactorSecret
            };
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<ActionResult<Enable2FAResponse>> Enable2FA([FromBody] Enable2FARequest request)
        {
            // Try to get userId from temp token (for first-time setup) or regular token
            var userId = GetUserIdFromToken();

            // If no user from regular token, try temp token
            if (userId == null)
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring(7);
                    userId = ValidateTempToken(token);
                }
            }

            if (userId == null)
                return Unauthorized();

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            // Verify password
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt!))
                return Unauthorized("Invalid password.");

            if (user.TwoFactorEnabled)
                return BadRequest("2FA is already enabled.");

            // Generate secret and backup codes
            var secret = TwoFactorHelper.GenerateSecret();
            var backupCodes = TwoFactorHelper.GenerateBackupCodes(10);
            var qrCodeUri = TwoFactorHelper.GetProvisioningUri(user.Email, secret);

            // Store temporarily (not enabled yet until verified)
            user.TwoFactorSecret = secret;
            user.BackupCodes = backupCodes;

            await _context.SaveChangesAsync();

            return new Enable2FAResponse
            {
                Secret = secret,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes
            };
        }

        [Authorize]
        [HttpPost("2fa/verify-setup")]
        public async Task<ActionResult> Verify2FASetup([FromBody] Verify2FASetupRequest request)
        {
            // Try to get userId from temp token or regular token
            var userId = GetUserIdFromToken();

            // If no user from regular token, try temp token
            if (userId == null)
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring(7);
                    userId = ValidateTempToken(token);
                }
            }

            if (userId == null)
                return Unauthorized();

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return BadRequest("2FA setup not initiated.");

            // Verify the code
            if (!TwoFactorHelper.ValidateCode(user.TwoFactorSecret, request.Code))
                return BadRequest("Invalid code. Please try again.");

            // Enable 2FA
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "2FA enabled successfully." });
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<ActionResult> Disable2FA([FromBody] Disable2FARequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            // Verify password
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt!))
                return Unauthorized("Invalid password.");

            if (!user.TwoFactorEnabled)
                return BadRequest("2FA is not enabled.");

            // Verify 2FA code
            if (!TwoFactorHelper.ValidateCode(user.TwoFactorSecret!, request.Code))
                return BadRequest("Invalid 2FA code.");

            // Disable 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.BackupCodes = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "2FA disabled successfully." });
        }

        [Authorize]
        [HttpGet("2fa/status")]
        public async Task<ActionResult> Get2FAStatus()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                enabled = user.TwoFactorEnabled,
                backupCodesCount = user.BackupCodes?.Count ?? 0
            });
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateTempToken(long userId, string email)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("temp", "true")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private long? ValidateTempToken(string token)
        {
            try
            {
                var jwtSection = _config.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                // Check if it's a temp token
                var tempClaim = principal.Claims.FirstOrDefault(c => c.Type == "temp");
                if (tempClaim?.Value != "true")
                    return null;

                // Extract userId
                var userIdClaim = principal.Claims.FirstOrDefault(c =>
                    c.Type == JwtRegisteredClaimNames.Sub ||
                    c.Type == ClaimTypes.NameIdentifier
                );

                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                    return userId;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private long? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                return userId;
            return null;
        }
    }
}

