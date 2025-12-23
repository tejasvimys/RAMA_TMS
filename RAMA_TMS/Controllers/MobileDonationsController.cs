using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Models;
using RAMA_TMS.Models.Donations;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/mobile/donations")]
    public class MobileDonationsController : ControllerBase
    {
        private readonly TMSDBContext _context;

        public MobileDonationsController(TMSDBContext context)
        {
            _context = context;
        }

        // POST: api/mobile/donations
        [Authorize] // JWT + role enforcement will be added when auth is wired
        [HttpPost]
        public async Task<ActionResult<MobileDonationResponse>> CreateDonation([FromBody] MobileDonationRequest request)
        {
            if (request.DonationAmt <= 0)
                return BadRequest("Donation amount must be positive.");

            var donor = await _context.DonorMasters
                .FirstOrDefaultAsync(d => d.DonorId == request.DonorId && d.IsActive);

            if (donor == null)
                return BadRequest("Invalid donor.");

            // Get current app user id from JWT ("sub" claim)
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!long.TryParse(userIdClaim, out var appUserId))
                return Unauthorized();

            var now = DateTimeOffset.UtcNow;

            var receipt = new DonorReceiptDetail
            {
                DonorId = donor.DonorId,
                Donor = donor,
                DonationAmt = request.DonationAmt,
                DonationType = request.DonationType,
                DateOfDonation = request.DateOfDonation, // assume already UTC or year-validated elsewhere
                PaymentMethod = request.PaymentMode,
                PaymentReference = request.ReferenceNo,
                InternalNotes = request.Notes,
                IsActive = true,
                CreatedBy = "mobile quick donation",
                CreatedDate = now,
                CollectedByUserId = null
            };

            _context.DonorReceiptDetails.Add(receipt);
            await _context.SaveChangesAsync();

            var response = new MobileDonationResponse
            {
                DonorReceiptDetailId = receipt.DonorReceiptDetailId,
                DonorId = receipt.DonorId,
                DonationAmt = receipt.DonationAmt,
                DateOfDonation = receipt.DateOfDonation
            };

            return Ok(response);
        }

        // GET: api/mobile/donations
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MobileDonationListItem>>> GetMyDonations()
        {
            // Try multiple claim types to be safe
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var appUserId))
            {
                return Unauthorized("Invalid user claim.");
            }

            var items = await _context.DonorReceiptDetails
                .Where(r => r.IsActive && r.CollectedByUserId == appUserId)
                .OrderByDescending(r => r.DateOfDonation)
                .ThenByDescending(r => r.DonorReceiptDetailId)
                .Select(r => new MobileDonationListItem
                {
                    DonorReceiptDetailId = r.DonorReceiptDetailId,
                    DonorId = r.DonorId,
                    DonorName = (r.Donor.FirstName + " " + r.Donor.LastName).Trim(),
                    DonationAmt = r.DonationAmt,
                    DonationType = r.DonationType,
                    DateOfDonation = r.DateOfDonation,
                    PaymentMode = r.PaymentMethod,
                    ReferenceNo = r.PaymentReference,
                    Notes = r.InternalNotes
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/mobile/donations/today
        [Authorize]
        [HttpGet("today")]
        public async Task<ActionResult<DaySummaryDto>> GetTodaySummary()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var appUserId))
            {
                return Unauthorized("Invalid user claim.");
            }

            var utcToday = DateTime.UtcNow.Date;

            var query = _context.DonorReceiptDetails
                .Where(r => r.IsActive &&
                            r.CollectedByUserId == appUserId &&
                            r.DateOfDonation.Date == utcToday);

            var total = await query.SumAsync(r => (decimal?)r.DonationAmt) ?? 0m;
            var count = await query.CountAsync();

            return new DaySummaryDto
            {
                Date = utcToday,
                TotalAmount = total,
                Count = count
            };
        }


    }
}
