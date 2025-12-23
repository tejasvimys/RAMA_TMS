using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
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

    }
}
