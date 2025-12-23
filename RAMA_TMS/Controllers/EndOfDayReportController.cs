using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAMA_TMS.Interface;
using RAMA_TMS.Models.Reports;
using System.Security.Claims;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class EndOfDayReportController : ControllerBase
    {
        private readonly IEndOfDayReportService _reportService;
        private readonly IEmailService _emailService;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly ILogger<EndOfDayReportController> _logger;

        public EndOfDayReportController(
            IEndOfDayReportService reportService,
            IEmailService emailService,
            IPdfGeneratorService pdfGenerator,
            ILogger<EndOfDayReportController> logger)
        {
            _reportService = reportService;
            _emailService = emailService;
            _pdfGenerator = pdfGenerator;
            _logger = logger;
        }

        [HttpGet("end-of-day")]
        public async Task<ActionResult<EndOfDayReportDto>> GetEndOfDayReport([FromQuery] string date)
        {
            try
            {
                _logger.LogInformation("GetEndOfDayReport called with date: {Date}", date);

                if (!DateTime.TryParse(date, out var reportDate))
                {
                    _logger.LogWarning("Invalid date format: {Date}", date);
                    return BadRequest("Invalid date format. Use yyyy-MM-dd");
                }

                // Get user info from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized("Invalid authentication token");
                }

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userIdClaim);
                    return BadRequest("Invalid user ID");
                }

                _logger.LogInformation("User: {UserId}, Role: {Role}", userId, roleClaim);

                // Get report filtered by user role
                var report = await _reportService.GetReportAsync(reportDate, userId, roleClaim);

                if (report == null)
                {
                    _logger.LogWarning("No report found for date {Date}, user {UserId}", date, userId);
                    return NotFound("No report found for the specified date");
                }

                _logger.LogInformation("Report generated successfully for date {Date}, user {UserId}", date, userId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting end of day report for date {Date}", date);
                return StatusCode(500, new
                {
                    message = "An error occurred while generating the report",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendReportEmail([FromBody] SendReportEmailRequest request)
        {
            try
            {
                if (!DateTime.TryParse(request.Date, out var reportDate))
                {
                    return BadRequest("Invalid date format. Use yyyy-MM-dd");
                }

                // Get user info from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid authentication token");
                }

                var report = await _reportService.GetReportAsync(reportDate, userId, roleClaim);

                if (report == null)
                {
                    return NotFound("No report found for the specified date");
                }

                // Generate PDF
                var pdfBytes = await _pdfGenerator.GenerateEndOfDayReportPdfAsync(report, reportDate);

                // Get admin emails from database
                var adminEmails = await _reportService.GetAdminEmailsAsync();

                if (!adminEmails.Any())
                {
                    return BadRequest("No admin email addresses configured");
                }

                // Send email with PDF attachment
                await _emailService.SendEndOfDayReportAsync(
                    adminEmails,
                    report,
                    reportDate,
                    pdfBytes
                );

                _logger.LogInformation("End of day report sent successfully for date {Date} to {Count} admins",
                    reportDate, adminEmails.Count);

                return Ok(new
                {
                    message = "Report sent successfully",
                    recipientCount = adminEmails.Count,
                    recipients = adminEmails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending end of day report email for date {Date}", request.Date);
                return StatusCode(500, "An error occurred while sending the report");
            }
        }
    }

    public class SendReportEmailRequest
    {
        public string Date { get; set; }
    }
}
