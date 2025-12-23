using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using RAMA_TMS.Models.Donations;
using System.Globalization;
using System.Security.Claims;
using System.Text;

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

        [HttpPost("quick")]
        public async Task<ActionResult<QuickDonationResponse>> CreateDonorAndDonation(
     [FromBody] QuickDonorAndDonationRequest request)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            if (request.Donor == null || request.Donation == null)
                return BadRequest("Donor and Donation are required.");

            var donorDto = request.Donor;
            var donationDto = request.Donation;

            if (string.IsNullOrWhiteSpace(donorDto.FirstName) ||
                string.IsNullOrWhiteSpace(donorDto.LastName))
                return BadRequest("Donor first and last name are required.");

            if (donationDto.DonationAmt <= 0)
                return BadRequest("Donation amount must be greater than zero.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) Try to find existing active donor by email or phone (same rule as search)
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

                // 2) If no existing donor, create a new one
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
                        CreatedBy = "quick-donation",
                        CreatedDate = nowUtc
                    };

                    _context.DonorMasters.Add(donor);
                    await _context.SaveChangesAsync(); // generates DonorId
                }

                var donationDateUtc = ToUtcOffset(donationDto.DateOfDonation); // if DateOfDonation is DateTime
                                                                               // or, if DateOfDonation is DateTimeOffset:
                var donationDateUtcOffset = donationDto.DateOfDonation.ToUniversalTime(); // force offset 0


                // 3) Always create a new donation for that donor
                var detail = new DonorReceiptDetail
                {
                    DonorId = donor.DonorId,
                    DonationAmt = donationDto.DonationAmt,
                    DonationType = donationDto.DonationType,
                    DateOfDonation = donationDateUtcOffset,
                    PaymentMethod = donationDto.PaymentMode,
                    PaymentReference = donationDto.ReferenceNo,
                    //Notes = donationDto.Notes, // if you added this column
                    IsActive = true,
                    CreatedBy = "quick-donation",
                    CreatedDate = nowUtc
                };

                _context.DonorReceiptDetails.Add(detail);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                var response = new QuickDonationResponse
                {
                    DonorId = donor.DonorId,
                    DonorReceiptDetailId = detail.DonorReceiptDetailId,
                    DonorFullName = $"{donor.FirstName} {donor.LastName}".Trim(),
                    DonationAmt = detail.DonationAmt,
                    DateOfDonation = donationDateUtcOffset
                };

                return CreatedAtAction(nameof(GetReceipt), new { id = detail.DonorReceiptDetailId }, response);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [Authorize]
        [HttpPost("quick-with-receipt")]
        public async Task<IActionResult> CreateDonorDonationAndSendReceipt(
    [FromBody] QuickDonorAndDonationRequest request)
        {
            if (request.Donor == null || request.Donation == null)
                return BadRequest("Donor and Donation are required.");

            var donorDto = request.Donor;
            var donationDto = request.Donation;

            if (string.IsNullOrWhiteSpace(donorDto.FirstName) ||
                string.IsNullOrWhiteSpace(donorDto.LastName))
                return BadRequest("Donor first and last name are required.");

            if (donationDto.DonationAmt <= 0)
                return BadRequest("Donation amount must be greater than zero.");

            // NEW: Validate reference number for non-cash payments
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

            // 1) Find or create donor (as earlier)
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
                    CreatedBy = collectedByUserId.HasValue ? $"collector:{collectedByUserId}" : "quick-donation"
                };

                _context.DonorMasters.Add(donor);
                await _context.SaveChangesAsync();
            }

            // normalize donation date to UTC offset 0 if needed
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
               // No = donationDto.Notes,
                IsActive = true,
                CreatedDate = nowUtc,
                CreatedBy = collectedByUserId.HasValue ? $"collector:{collectedByUserId}" : "quick-donation",
                CollectedByUserId = collectedByUserId
            };

            _context.DonorReceiptDetails.Add(receipt);
            await _context.SaveChangesAsync();

            await tx.CommitAsync(); // at this point donor + donation are in DB

            // 3) Generate PDF and send email
            if (string.IsNullOrWhiteSpace(donor.Email))
                return BadRequest("Donation saved but donor email is missing.");

            var pdfBytes = _pdfGenerator.GenerateReceipt(receipt, donor);

            var fileName = $"DonationReceipt-{receipt.Donor.FirstName + " " + receipt.Donor.LastName}.pdf";

            var subject = $"RAMA Donation Receipt - {receipt.DateOfDonation.Year}";
            var body = "Hare Srinivasa! Please find attached your donation receipt. Thank you very much for your donation! RAMA";

            await _emailService.SendReceiptAsync(
                donor.Email,
                subject,
                body,
                pdfBytes);

            // 4) Return PDF to client for download
            return File(pdfBytes, "application/pdf", fileName);
        }


        // GET: api/donorreceipts
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<DonationListDto>>> GetDonations(
    [FromQuery] DonationListQueryDto query)
        {
            if (query.Year <= 0)
                return BadRequest("Year is required.");

            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 25;

            var baseQuery = _context.DonorReceiptDetails
                .Include(r => r.Donor)
                .Where(r => r.IsActive && r.DateOfDonation.Year == query.Year);

            // search on donor name / email / notes
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                baseQuery = baseQuery.Where(r =>
                    (r.Donor.FirstName + " " + r.Donor.LastName).ToLower().Contains(s) ||
                    (r.Donor.Email != null && r.Donor.Email.ToLower().Contains(s)));
            }

            // sorting
            var dirDesc = string.Equals(query.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            baseQuery = query.Sort?.ToLower() switch
            {
                "amount" => dirDesc
                    ? baseQuery.OrderByDescending(r => r.DonationAmt)
                    : baseQuery.OrderBy(r => r.DonationAmt),
                "donor" => dirDesc
                    ? baseQuery.OrderByDescending(r => r.Donor.LastName).ThenByDescending(r => r.Donor.FirstName)
                    : baseQuery.OrderBy(r => r.Donor.LastName).ThenBy(r => r.Donor.FirstName),
                "date" or "dateofdonation" => dirDesc
                    ? baseQuery.OrderByDescending(r => r.DateOfDonation)
                    : baseQuery.OrderBy(r => r.DateOfDonation),
                _ => dirDesc
                    ? baseQuery.OrderByDescending(r => r.DonorReceiptDetailId)   // default
                    : baseQuery.OrderBy(r => r.DonorReceiptDetailId)
            };

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new DonationListDto
                {
                    DonorReceiptDetailId = r.DonorReceiptDetailId,
                    DonorId = r.DonorId,
                    DonationAmt = r.DonationAmt,
                    DonationType = r.DonationType,
                    DateOfDonation = r.DateOfDonation,
                    PaymentMode = r.PaymentMethod,
                    ReferenceNo = r.PaymentReference,
                    DonorFirstName = r.Donor.FirstName,
                    DonorLastName = r.Donor.LastName,
                    DonorEmail = r.Donor.Email,
                     //CollectedByName = r.CollectedByUser != null ? r.CollectedByUser.DisplayName: null
                })
                .ToListAsync();

            return Ok(new PagedResultDto<DonationListDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                Items = items
            });
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportByYear([FromQuery] int year)
        {
            if (year <= 0)
                return BadRequest("Year is required.");

            var rows = await _context.DonorReceiptDetails
                .Include(r => r.Donor)
                .Where(r => r.IsActive && r.DateOfDonation.Year == year)
                .OrderBy(r => r.DateOfDonation)
                .ThenBy(r => r.DonorReceiptDetailId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ReceiptId,Date,DonorName,Email,Amount,Type,PaymentMode,ReferenceNo");

            string CsvEscape(string? v) =>
                v == null ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";

            foreach (var r in rows)
            {
                var donorName = $"{r.Donor.FirstName} {r.Donor.LastName}".Trim();
                var date = r.DateOfDonation.ToString("yyyy-MM-dd");
                var amt = r.DonationAmt.ToString("0.00", CultureInfo.InvariantCulture);

                sb.AppendLine(string.Join(",",
                    r.DonorReceiptDetailId,
                    date,
                    CsvEscape(donorName),
                    CsvEscape(r.Donor.Email),
                    amt,
                    CsvEscape(r.DonationType),
                    CsvEscape(r.PaymentMethod),
                    CsvEscape(r.PaymentReference)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"RAMA-Donations-{year}.csv";
            return File(bytes, "text/csv", fileName);  // File(...) is the key for downloads [web:325][web:337]
        }


        private static DateTimeOffset ToUtcOffset(DateTime date)
        {
            // treat incoming date as local or unspecified, then force UTC
            var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return new DateTimeOffset(utc); // offset 0
        }
    }
}
