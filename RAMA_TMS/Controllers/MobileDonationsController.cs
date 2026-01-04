using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using RAMA_TMS.Models.Donations;
using RAMA_TMS.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/mobile/donations")]
    public class MobileDonationsController : ControllerBase
    {
        private readonly TMSDBContext _context;
        private readonly IDonationReceiptPdfGenerator _pdfGenerator;
        private readonly IEmailService _emailService;


        public MobileDonationsController(TMSDBContext context, IDonationReceiptPdfGenerator pdfGenerator, IEmailService emailService)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _emailService = emailService;
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

        // GET /api/mobile/donations
        [Authorize]
        [HttpGet("donations")]
        public async Task<ActionResult<List<MobileDonationListItem>>> GetMyDonations()
        {
            Console.WriteLine("📥 GetMyDonations endpoint called");

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            Console.WriteLine($"🔑 User ID Claim: {userIdClaim}");
            Console.WriteLine($"📧 User Email: {userEmail}");

            if (string.IsNullOrEmpty(userIdClaim) && string.IsNullOrEmpty(userEmail))
                return Unauthorized("User credentials not found in token.");

            try
            {
                // Parse user ID
                long userId = 0;
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    long.TryParse(userIdClaim, out userId);
                }

                Console.WriteLine($"👤 Parsed User ID: {userId}");

                // Get ALL active donations first (for debugging)
                var allDonations = await _context.DonorReceiptDetails
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.DateOfDonation)
                    .Take(100)
                    .Select(r => new
                    {
                        r.DonorReceiptDetailId,
                        r.CreatedBy,
                        r.CollectedByUserId,
                        r.DonationAmt,
                        r.DateOfDonation
                    })
                    .ToListAsync();

                Console.WriteLine($"📊 Total active donations in DB: {allDonations.Count}");

                // Log first few for debugging
                foreach (var d in allDonations.Take(5))
                {
                    Console.WriteLine($"   - Receipt {d.DonorReceiptDetailId}: CreatedBy='{d.CreatedBy}', CollectedBy={d.CollectedByUserId}, Amount=${d.DonationAmt}");
                }

                // Now filter for this user
                var query = _context.DonorReceiptDetails
                    .Where(r => r.IsActive);

                // Try multiple matching strategies
                if (userId > 0)
                {
                    Console.WriteLine($"🔍 Filtering by User ID: {userId}");
                    query = query.Where(r =>
                        r.CollectedByUserId == userId ||
                        (r.CreatedBy != null && (
                            r.CreatedBy.Contains($"mobile:{userId}") ||
                            r.CreatedBy.Contains($"collector:{userId}")
                        ))
                    );
                }
                else if (!string.IsNullOrEmpty(userEmail))
                {
                    Console.WriteLine($"🔍 Filtering by Email: {userEmail}");
                    query = query.Where(r =>
                        r.CreatedBy != null && r.CreatedBy.ToLower().Contains(userEmail.ToLower())
                    );
                }
                else
                {
                    return BadRequest("Unable to identify user from token.");
                }

                var donations = await query
                    .OrderByDescending(r => r.DateOfDonation)
                    .Select(r => new MobileDonationListItem
                    {
                        DonorReceiptDetailId = r.DonorReceiptDetailId,
                        DonorId = r.DonorId,
                        DonorName = r.Donor != null ? $"{r.Donor.FirstName} {r.Donor.LastName}" : "Unknown",
                        DonationAmt = r.DonationAmt,
                        DonationType = r.DonationType,
                        DateOfDonation = r.DateOfDonation,
                        PaymentMode = r.PaymentMethod,
                        ReferenceNo = r.PaymentReference,
                        Notes = null
                    })
                    .ToListAsync();

                Console.WriteLine($"✅ Returning {donations.Count} donations for user");

                return Ok(donations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading donations: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Failed to load donations: {ex.Message}");
            }
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

        // ✅ NEW: POST /api/mobile/donations/quick
        // Mobile-specific endpoint that returns JSON (not PDF)
        [HttpPost("quick")]
        public async Task<ActionResult<MobileQuickDonationResponse>> CreateQuickDonation(
            [FromBody] QuickDonorAndDonationRequest request)
        {
            if (request.Donor == null || request.Donation == null)
                return BadRequest("Donor and Donation are required.");

            var donorDto = request.Donor;
            var donationDto = request.Donation;

            // Validation
            if (string.IsNullOrWhiteSpace(donorDto.FirstName) ||
                string.IsNullOrWhiteSpace(donorDto.LastName))
                return BadRequest("Donor first and last name are required.");

            if (donationDto.DonationAmt <= 0)
                return BadRequest("Donation amount must be greater than zero.");

            // Validate reference number for non-cash payments
            if (!string.Equals(donationDto.PaymentMode, "Cash", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(donationDto.ReferenceNo))
                {
                    return BadRequest($"Reference number is required for {donationDto.PaymentMode} payments.");
                }
            }

            // Extract user ID from JWT
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            long? collectedByUserId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
            {
                collectedByUserId = userId;
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) Find or create donor
                DonorMaster? donor = null;
                if (!string.IsNullOrWhiteSpace(donorDto.Email) ||
                    !string.IsNullOrWhiteSpace(donorDto.Phone))
                {
                    var query = _context.DonorMasters.Where(d => d.IsActive);

                    if (!string.IsNullOrWhiteSpace(donorDto.Email))
                    {
                        var email = donorDto.Email.Trim();
                        query = query.Where(d => d.Email != null && d.Email.ToLower() == email.ToLower());
                    }

                    if (!string.IsNullOrWhiteSpace(donorDto.Phone))
                    {
                        var phone = donorDto.Phone.Trim();
                        query = query.Where(d => d.Phone != null && d.Phone == phone);
                    }

                    donor = await query
                        .OrderBy(d => d.LastName)
                        .ThenBy(d => d.FirstName)
                        .FirstOrDefaultAsync();
                }

                var nowUtc = DateTimeOffset.UtcNow;

                if (donor == null)
                {
                    donor = new DonorMaster
                    {
                        FirstName = donorDto.FirstName.Trim(),
                        LastName = donorDto.LastName.Trim(),
                        Phone = donorDto.Phone?.Trim(),
                        Email = donorDto.Email?.Trim(),
                        Address1 = donorDto.Address1,
                        Address2 = donorDto.Address2,
                        City = donorDto.City,
                        State = donorDto.State,
                        Country = donorDto.Country,
                        PostalCode = donorDto.PostalCode,
                        IsOrganization = donorDto.IsOrganization,
                        OrganizationName = donorDto.OrganizationName,
                        DonorType = donorDto.DonorType,
                        IsActive = true,
                        CreatedDate = nowUtc,
                        CreatedBy = collectedByUserId.HasValue ? $"mobile:{collectedByUserId}" : "mobile-app"
                    };

                    _context.DonorMasters.Add(donor);
                    await _context.SaveChangesAsync();
                }

                var donationDateUtc = donationDto.DateOfDonation.ToUniversalTime();

                // 2) Create donation/receipt
                var receipt = new DonorReceiptDetail
                {
                    DonorId = donor.DonorId,
                    DonationAmt = donationDto.DonationAmt,
                    DonationType = donationDto.DonationType,
                    DateOfDonation = donationDateUtc,
                    PaymentMethod = donationDto.PaymentMode,
                    PaymentReference = donationDto.ReferenceNo,
                    IsActive = true,
                    CreatedDate = nowUtc,
                    CreatedBy = collectedByUserId.HasValue ? $"mobile:{collectedByUserId}" : "mobile-app",
                    CollectedByUserId = collectedByUserId
                };

                _context.DonorReceiptDetails.Add(receipt);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // 3) Send email asynchronously (don't block response)
                bool emailSent = false;
                if (!string.IsNullOrWhiteSpace(donor.Email))
                {
                    emailSent = true;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var pdfBytes = _pdfGenerator.GenerateReceipt(receipt, donor);
                            var subject = $"RAMA Donation Receipt - {receipt.DateOfDonation.Year}";
                            var body = "Hare Srinivasa! Please find attached your donation receipt. Thank you very much for your donation! RAMA";

                            await _emailService.SendReceiptAsync(donor.Email, subject, body, pdfBytes);
                            Console.WriteLine($"✅ Email sent to {donor.Email} for receipt {receipt.DonorReceiptDetailId}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to send email: {ex.Message}");
                        }
                    });
                }

                // 4) ✅ Return mobile-friendly JSON response
                var response = new MobileQuickDonationResponse
                {
                    DonorId = donor.DonorId,
                    DonorReceiptDetailId = receipt.DonorReceiptDetailId,
                    DonorFullName = $"{donor.FirstName} {donor.LastName}".Trim(),
                    DonationAmt = receipt.DonationAmt,
                    DateOfDonation = donationDateUtc,
                    ReceiptNumber = $"R{receipt.DonorReceiptDetailId:D6}",
                    DonationType = receipt.DonationType,
                    PaymentMethod = receipt.PaymentMethod ?? "Cash",
                    PaymentReference = receipt.PaymentReference,
                    EmailSent = emailSent,
                    ReceiptPdfUrl = $"/api/mobile/receipts/{receipt.DonorReceiptDetailId}/pdf"
                };

                return Ok(response);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ✅ NEW: GET /api/mobile/receipts/{id}/pdf
        // Download PDF receipt for mobile
        [HttpGet("receipts/{id:long}/pdf")]
        public async Task<IActionResult> GetReceiptPdf(long id)
        {
            var receipt = await _context.DonorReceiptDetails
                .Include(r => r.Donor)
                .FirstOrDefaultAsync(r => r.DonorReceiptDetailId == id && r.IsActive);

            if (receipt == null)
                return NotFound($"Receipt with ID {id} not found.");

            if (receipt.Donor == null)
                return NotFound("Donor information not found for this receipt.");

            try
            {
                // Generate PDF
                var pdfBytes = _pdfGenerator.GenerateReceipt(receipt, receipt.Donor);

                var fileName = $"DonationReceipt-{receipt.Donor.FirstName}_{receipt.Donor.LastName}-R{receipt.DonorReceiptDetailId:D6}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF: {ex.Message}");
                return StatusCode(500, "Failed to generate PDF receipt.");
            }
        }
    }
}
