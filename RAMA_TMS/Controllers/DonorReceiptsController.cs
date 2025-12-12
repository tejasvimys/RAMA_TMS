using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonorReceiptsController : Controller
    {
        private readonly TMSDBContext _context;
        private readonly IDonationReceiptPdfGenerator _pdfGenerator;
        private readonly IEmailService _emailService;
        public DonorReceiptsController(TMSDBContext context, IDonationReceiptPdfGenerator pdfGenerator,
    IEmailService emailService)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _emailService = emailService;
        }

        // GET: api/donorreceipts/by-donor/5
        [HttpGet("by-donor/{donorId:long}")]
        public async Task<ActionResult<IEnumerable<DonorReceiptDetail>>> GetByDonor(long donorId)
        {
            var receipts = await _context.DonorReceiptDetails
                .Where(r => r.DonorId == donorId)
                .OrderByDescending(r => r.DateOfDonation)
                .ToListAsync();

            return Ok(receipts);
        }

        // POST: api/donorreceipts
        [HttpPost]
        public async Task<ActionResult<DonorReceiptDetail>> CreateReceipt([FromBody] DonorReceiptDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (dto.DonorId <= 0)
                ModelState.AddModelError(nameof(dto.DonorId), "DonorId is required.");

            if (dto.DonationAmt <= 0)
                ModelState.AddModelError(nameof(dto.DonationAmt), "Donation amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.DonationType))
                ModelState.AddModelError(nameof(dto.DonationType), "Donation type is required.");

            if (string.IsNullOrWhiteSpace(dto.Currency))
                ModelState.AddModelError(nameof(dto.Currency), "Currency is required.");

            var donationDate = dto.DateOfDonation ?? DateTimeOffset.UtcNow;

            // Optional: disallow future dates beyond a small tolerance
            if (donationDate > DateTimeOffset.UtcNow.AddMinutes(5))
                ModelState.AddModelError(nameof(dto.DateOfDonation), "Date of donation cannot be in the far future.");

            if (!string.IsNullOrWhiteSpace(dto.PaymentMethod) && dto.PaymentMethod.Length > 20)
                ModelState.AddModelError(nameof(dto.PaymentMethod), "Payment method is too long.");

            if (!string.IsNullOrWhiteSpace(dto.PaymentReference) && dto.PaymentReference.Length > 100)
                ModelState.AddModelError(nameof(dto.PaymentReference), "Payment reference is too long.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Verify donor exists
            var donorExists = await _context.DonorMasters
                .AnyAsync(d => d.DonorId == dto.DonorId);

            if (!donorExists)
                return BadRequest($"Donor with id {dto.DonorId} does not exist.");

            var detail = new DonorReceiptDetail
            {
                DonorId = dto.DonorId,
                DonationAmt = dto.DonationAmt,
                DonationType = dto.DonationType.Trim(),
                Currency = dto.Currency.Trim(),
                DateOfDonation = donationDate,
                PaymentMethod = dto.PaymentMethod?.Trim(),
                PaymentReference = dto.PaymentReference?.Trim(),
                IsTaxDeductible = dto.IsTaxDeductible,
                IsAnonymous = dto.IsAnonymous,
                InternalNotes = dto.InternalNotes,
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedBy = "system"
            };

            _context.DonorReceiptDetails.Add(detail);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReceipt),
                new { id = detail.DonorReceiptDetailId }, detail);
        }

        // GET: api/donorreceipts/5
        [HttpGet("{id:long}")]
        public async Task<ActionResult<DonorReceiptDetail>> GetReceipt(long id)
        {
            var receipt = await _context.DonorReceiptDetails.FindAsync(id);

            if (receipt == null)
                return NotFound();

            return Ok(receipt);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<DonorMaster>>> SearchDonors(
    [FromQuery] string? phone,
    [FromQuery] string? email,
     [FromQuery] long? donorId)
        {
            // Basic presence validation
            if (donorId is null && string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Provide at least one of donorId, phone, or email to search.");
            }

            // Field-level validation
            if (donorId is not null && donorId <= 0)
            {
                ModelState.AddModelError(nameof(donorId), "donorId must be greater than zero.");
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 25)
            {
                ModelState.AddModelError(nameof(phone), "Phone number is too long.");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (email.Length > 255 || !email.Contains("@"))
                {
                    ModelState.AddModelError(nameof(email), "Email must be a valid email address.");
                }
            }

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            IQueryable<DonorMaster> query = _context.DonorMasters;

            if (donorId is not null)
            {
                query = query.Where(d => d.DonorId == donorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var normalizedPhone = phone.Trim();
                query = query.Where(d => d.Phone != null && d.Phone == normalizedPhone);
                // For partial matches, change to:
                // query = query.Where(d => d.Phone != null && d.Phone.Contains(normalizedPhone));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                query = query.Where(d => d.Email != null && d.Email.ToLower() == normalizedEmail);
            }

            var donors = await query
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            if (donors.Count == 0)
            {
                return NotFound();
            }

            return Ok(donors);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> SoftDeleteReceipt(long id)
        {
            var receipt = await _context.DonorReceiptDetails.FindAsync(id);
            if (receipt == null)
                return NotFound();

            receipt.IsActive = false;
            receipt.UpdateDate = DateTimeOffset.UtcNow;
            receipt.UpdatedBy = "system";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("api/donations/{receiptId}/send-receipt")]
        public async Task<IActionResult> SendReceipt(long receiptId)
        {
            var receipt = await _context.DonorReceiptDetails
                .Include(r => r.Donor)
                .FirstOrDefaultAsync(r => r.DonorReceiptDetailId == receiptId);

            if (receipt == null || receipt.Donor == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(receipt.Donor.Email))
                return BadRequest("Donor email is missing.");

            var pdfBytes = _pdfGenerator.GenerateReceipt(receipt, receipt.Donor);

            var subject = $"Donation Receipt - {receipt.DateOfDonation.Year}";
            var body = "Please find attached your donation receipt.";

            await _emailService.SendReceiptAsync(receipt.Donor.Email, subject, body, pdfBytes);

            return Ok();
        }

    }
}
