using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAMA_TMS.Data;
using RAMA_TMS.Interface;
using RAMA_TMS.Models.Auth;
using RAMA_TMS.Models.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RAMA_TMS.Helpers;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TMSDBContext _context;
        private readonly IConfiguration _config;
        private readonly IGoogleTokenValidator _googleValidator;

        public AuthController(TMSDBContext context, IConfiguration config, IGoogleTokenValidator googleValidator)
        {
            _context = context;
            _config = config;
            _googleValidator = googleValidator;
        }

        public AuthController(TMSDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("exchange")]
        public async Task<ActionResult<TokenExchangeResponse>> Exchange([FromBody] TokenExchangeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest("IdToken is required.");

            if (!string.Equals(request.Provider, "google", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Unsupported provider.");

            var email = await _googleValidator.ValidateAndGetEmailAsync(request.IdToken);
            if (email == null)
                return Unauthorized("Invalid Google token.");

            var displayName = email.Split('@').FirstOrDefault() ?? email;

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            var isNewUser = false;

            if (appUser == null)
            {
                appUser = new AppUser
                {
                    Email = email,
                    DisplayName = displayName,
                    Role = "Collector",
                    IsActive = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "self-registration"
                };

                _context.AppUsers.Add(appUser);
                await _context.SaveChangesAsync();

                isNewUser = true;
            }

            if (!appUser.IsActive)
            {
                return new TokenExchangeResponse
                {
                    AppToken = null,
                    Email = appUser.Email,
                    DisplayName = appUser.DisplayName,
                    Role = appUser.Role,
                    IsActive = false,
                    IsNewUser = isNewUser
                };
            }

            // Issue JWT
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, appUser.Id.ToString()),  // CRITICAL
        new Claim(JwtRegisteredClaimNames.Email, appUser.Email),
        new Claim(ClaimTypes.Role, appUser.Role),
        new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString())     // backup
    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenExchangeResponse
            {
                AppToken = tokenString,
                Email = appUser.Email,
                DisplayName = appUser.DisplayName,
                Role = appUser.Role,
                IsActive = true,
                IsNewUser = isNewUser
            };
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
                Role = "Collector", // default
                IsActive = false,   // admin must approve
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "self-registration"
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registered. Waiting for admin approval." });
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenExchangeResponse>> Login([FromBody] LoginRequest request)
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
                return new TokenExchangeResponse
                {
                    AppToken = null,
                    Email = appUser.Email,
                    DisplayName = appUser.DisplayName,
                    Role = appUser.Role,
                    IsActive = false,
                    IsNewUser = false
                };
            }

            // Issue JWT
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, appUser.Id.ToString()),  // CRITICAL: user ID as sub
        new Claim(JwtRegisteredClaimNames.Email, appUser.Email),
        new Claim(ClaimTypes.Role, appUser.Role),
        new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString())     // backup claim
    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenExchangeResponse
            {
                AppToken = tokenString,
                Email = appUser.Email,
                DisplayName = appUser.DisplayName,
                Role = appUser.Role,
                IsActive = true,
                IsNewUser = false
            };
        }

    }
}

