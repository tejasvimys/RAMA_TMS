using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly TMSDBContext _context;

        public HealthController(TMSDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> CheckHealth()
        {
            try
            {
                // Check database connectivity
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        message = "Database connection failed",
                        timestamp = DateTimeOffset.UtcNow
                    });
                }

                // Optional: Check if database has data (tables exist)
                var userCount = await _context.AppUsers.CountAsync();

                return Ok(new
                {
                    status = "healthy",
                    message = "API and Database are operational",
                    database = "connected",
                    timestamp = DateTimeOffset.UtcNow,
                    version = "1.0.0"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    message = "Health check failed",
                    error = ex.Message,
                    timestamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
