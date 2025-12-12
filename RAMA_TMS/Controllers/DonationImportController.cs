using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.DTO;
using RAMA_TMS.Models;
using RAMA_TMS.Models.Import;

namespace RAMA_TMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonationImportController : ControllerBase
    {
        private readonly TMSDBContext _context;
        private readonly ILogger<DonationImportController> _logger;

        public DonationImportController(TMSDBContext context, ILogger<DonationImportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/donationimport/dryrun?year=2024
        [HttpPost("dryrun")]
        [RequestSizeLimit(50_000_000)] // adjust if needed
        public async Task<ActionResult<DonationImportSummary>> DryRun(
            [FromQuery] int year,
            IFormFile file)
        {
            if (year <= 0)
                return BadRequest("Year is required.");

            if (file == null || file.Length == 0)
                return BadRequest("Upload file is required.");

            try
            {
                var summary = await ProcessFileAsync(year, file, persistChanges: false);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during donation import dry run for year {Year}", year);
                return StatusCode(500, "An error occurred during dry run.");
            }
        }

        // POST: api/donationimport?year=2024
        [HttpPost]
        [RequestSizeLimit(50_000_000)]
        public async Task<ActionResult<DonationImportSummary>> Import(
            [FromQuery] int year,
            IFormFile file)
        {
            if (year <= 0)
                return BadRequest("Year is required.");

            if (file == null || file.Length == 0)
                return BadRequest("Upload file is required.");

            try
            {
                var summary = await ProcessFileAsync(year, file, persistChanges: true);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during donation import for year {Year}", year);
                return StatusCode(500, "An error occurred during import.");
            }
        }

        private enum CsvFormat
        {
            Unknown,
            NewSeparatedName,
            OldFullName
        }

        /// <summary>
        /// Core import logic: parses file, matches/creates donors, builds DonationImportSummary.
        /// When persistChanges=false, no DB changes are committed.
        /// </summary>

        private async Task<DonationImportSummary> ProcessFileAsync(
    int year,
    IFormFile file,
    bool persistChanges)
        {
            var summary = new DonationImportSummary();
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            // Detect header format
            string? headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
                throw new Exception("File is empty.");

            var headerParts = headerLine
       .Split(',', StringSplitOptions.None)
       .Select(h => h.Trim().Trim('"'))
       .ToArray();

            CsvFormat format = CsvFormat.Unknown;

            // New format: Date,First Name,Last Name,email,Amount
            if (headerParts.Length >= 5 &&
                headerParts[0].Equals("Date", StringComparison.OrdinalIgnoreCase) &&
                headerParts[1].StartsWith("First", StringComparison.OrdinalIgnoreCase) &&
                headerParts[2].StartsWith("Last", StringComparison.OrdinalIgnoreCase) &&
                headerParts[3].Contains("mail", StringComparison.OrdinalIgnoreCase) &&
                headerParts[4].StartsWith("Amount", StringComparison.OrdinalIgnoreCase))
            {
                format = CsvFormat.NewSeparatedName;
            }
            // Old format: FullName,DonationAmount,Email,Phone,Date (phone/date optional)
            else if (headerParts.Length >= 3 &&
                     headerParts[0].Contains("Full", StringComparison.OrdinalIgnoreCase) &&
                     headerParts[1].Contains("Amount", StringComparison.OrdinalIgnoreCase) &&
                     headerParts[2].Contains("mail", StringComparison.OrdinalIgnoreCase))
            {
                format = CsvFormat.OldFullName;
            }

            if (format == CsvFormat.Unknown)
                throw new Exception("Unrecognized CSV header format.");

            // Preload donors for matching
            var donors = await _context.DonorMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .ToListAsync();

            var donorByEmail = donors
                .Where(d => !string.IsNullOrEmpty(d.Email))
                .GroupBy(d => d.Email!.ToLower())
                .ToDictionary(g => g.Key, g => g.ToList());

            var donorByPhone = donors
                .Where(d => !string.IsNullOrEmpty(d.Phone))
                .GroupBy(d => d.Phone!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var newDonorsToAdd = new List<DonorMaster>();
            var newReceiptsToAdd = new List<DonorReceiptDetail>();

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var rowResult = new DonationImportRowResult();

                try
                {
                    var parts = line.Split(',', StringSplitOptions.None);

                    string dateRaw = string.Empty;
                    string firstName = string.Empty;
                    string lastName = string.Empty;
                    string fullName = string.Empty;
                    string email = string.Empty;
                    string phone = string.Empty;
                    string amountRaw = string.Empty;

                    if (format == CsvFormat.NewSeparatedName)
                    {
                        // Date,First Name,Last Name,email,Amount
                        dateRaw = parts.Length > 0 ? parts[0].Trim().Trim('"') : string.Empty;
                        firstName = parts.Length > 1 ? parts[1].Trim().Trim('"') : string.Empty;
                        lastName = parts.Length > 2 ? parts[2].Trim().Trim('"') : string.Empty;
                        email = parts.Length > 3 ? parts[3].Trim().Trim('"') : string.Empty;
                        amountRaw = parts.Length > 4 ? parts[4].Trim().Trim('"') : string.Empty;
                        fullName = $"{firstName} {lastName}".Trim();
                    }
                    else
                    {
                        // Old format: FullName,DonationAmount,Email,Phone,Date
                        fullName = parts.Length > 0 ? parts[0].Trim().Trim('"') : string.Empty;
                        amountRaw = parts.Length > 1 ? parts[1].Trim().Trim('"') : string.Empty;
                        email = parts.Length > 2 ? parts[2].Trim().Trim('"') : string.Empty;
                        phone = parts.Length > 3 ? parts[3].Trim().Trim('"') : string.Empty;
                        dateRaw = parts.Length > 4 ? parts[4].Trim().Trim('"') : string.Empty;

                        // Split full name into first/last
                        var tokens = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length > 1)
                        {
                            firstName = string.Join(' ', tokens.Take(tokens.Length - 1));
                            lastName = tokens.Last();
                        }
                        else
                        {
                            firstName = fullName;
                            lastName = string.Empty;
                        }
                    }

                    rowResult.FullName = fullName;
                    rowResult.DonationAmountRaw = amountRaw;
                    rowResult.Email = email;
                    rowResult.Phone = phone;
                    rowResult.DateRaw = dateRaw;

                    // Basic validation
                    if (string.IsNullOrWhiteSpace(fullName))
                        throw new Exception("Name is required.");

                    if (string.IsNullOrWhiteSpace(email))
                        throw new Exception("Email is required.");
                    if (!email.Contains("@"))
                        throw new Exception("Email is invalid.");

                    if (string.IsNullOrWhiteSpace(amountRaw))
                        throw new Exception("Donation amount is required.");

                    var cleanedAmount = amountRaw.Replace("$", string.Empty)
                                                 .Replace(",", string.Empty)
                                                 .Trim();
                    if (!decimal.TryParse(cleanedAmount, out var amount) || amount <= 0)
                        throw new Exception("Donation amount is invalid.");

                    // Date handling
                    DateTimeOffset donationDate;
                    if (!string.IsNullOrWhiteSpace(dateRaw))
                    {
                        if (!DateTimeOffset.TryParse(dateRaw, out donationDate))
                            throw new Exception("Donation date is invalid.");

                        if (donationDate.Year != year)
                            throw new Exception($"Donation date year {donationDate.Year} does not match selected year {year}.");

                        donationDate = new DateTimeOffset(donationDate.UtcDateTime, TimeSpan.Zero);
                    }
                    else
                    {
                        // default 01-DEC-year in UTC
                        var local = new DateTime(year, 12, 1, 0, 0, 0, DateTimeKind.Utc);
                        donationDate = new DateTimeOffset(local, TimeSpan.Zero);
                    }

                    // Match or create donor (email is primary identity)
                    var (donor, isNewDonor) = MatchOrCreateDonor(
                        donors,
                        donorByEmail,
                        donorByPhone,
                        firstName,
                        lastName,
                        email,
                        phone);

                    if (isNewDonor)
                    {
                        newDonorsToAdd.Add(donor);
                        donors.Add(donor);

                        if (!string.IsNullOrEmpty(donor.Email))
                        {
                            var key = donor.Email!.ToLower();
                            if (!donorByEmail.ContainsKey(key))
                                donorByEmail[key] = new List<DonorMaster>();
                            donorByEmail[key].Add(donor);
                        }

                        if (!string.IsNullOrEmpty(donor.Phone))
                        {
                            var key = donor.Phone!;
                            if (!donorByPhone.ContainsKey(key))
                                donorByPhone[key] = new List<DonorMaster>();
                            donorByPhone[key].Add(donor);
                        }

                        summary.DonorsCreated++;
                    }
                    else
                    {
                        summary.DonorsMatched++;
                    }

                    // Update phone if missing in DB and present in CSV
                    if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(donor.Phone))
                    {
                        donor.Phone = phone;
                    }

                    // Create receipt with Donor navigation
                    var receipt = new DonorReceiptDetail
                    {
                        Donor = donor,
                        DonationAmt = amount,
                        DonationType = "General",
                        Currency = "USD",
                        DateOfDonation = donationDate,
                        PaymentMethod = "Cash",
                        PaymentReference = null,
                        IsTaxDeductible = true,
                        IsAnonymous = false,
                        InternalNotes = $"Bulk upload {year}",
                        CreatedDate = DateTimeOffset.UtcNow,
                        CreatedBy = "bulk-import"
                    };

                    newReceiptsToAdd.Add(receipt);
                    summary.DonationsImported++;

                    // Imported row preview (DonorId may be 0 for new donors, fixed after SaveChanges)
                    summary.ImportedRows.Add(new DonationImportImportedRowDto
                    {
                        DonorId = donor.DonorId,
                        DonorName = $"{donor.FirstName} {donor.LastName}".Trim(),
                        Email = donor.Email,
                        Phone = donor.Phone,
                        DonationAmount = amount,
                        DateOfDonation = donationDate,
                        Status = "Imported"
                    });
                }
                catch (Exception exRow)
                {
                    rowResult.ErrorMessage = exRow.Message;
                    summary.FailedRows.Add(rowResult);
                    summary.RowsFailed++;
                }
            }

            if (persistChanges)
            {
                // Add new donors
                if (newDonorsToAdd.Count > 0)
                {
                    _context.DonorMasters.AddRange(newDonorsToAdd);
                }

                // Attach existing donors referenced by receipts
                foreach (var receipt in newReceiptsToAdd)
                {
                    if (receipt.Donor != null && _context.Entry(receipt.Donor).State == EntityState.Detached)
                    {
                        if (receipt.Donor.DonorId != 0)
                        {
                            _context.DonorMasters.Attach(receipt.Donor);
                        }
                    }
                }

                // Add receipts
                _context.DonorReceiptDetails.AddRange(newReceiptsToAdd);

                await _context.SaveChangesAsync();

                // Fix DonorId in ImportedRows for new donors (where it was 0)
                foreach (var imported in summary.ImportedRows.Where(r => r.DonorId == 0))
                {
                    var donor = newDonorsToAdd.FirstOrDefault(d =>
                        string.Equals($"{d.FirstName} {d.LastName}".Trim(), imported.DonorName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.Email ?? string.Empty, imported.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                    if (donor != null)
                    {
                        imported.DonorId = donor.DonorId;
                    }
                }
            }

            return summary;
        }

        private static (DonorMaster donor, bool isNew) MatchOrCreateDonor(
    List<DonorMaster> donors,
    Dictionary<string, List<DonorMaster>> donorByEmail,
    Dictionary<string, List<DonorMaster>> donorByPhone,
    string firstName,
    string lastName,
    string email,
    string phone)
        {
            DonorMaster? match = null;

            var emailKey = email.Trim().ToLower();

            // 1) Match by email FIRST.
            //    This means same name + different email => DIFFERENT PERSONS.
            if (!string.IsNullOrWhiteSpace(email) &&
                donorByEmail.TryGetValue(emailKey, out var emailMatches) &&
                emailMatches.Count > 0)
            {
                // If multiple donors share the same email, pick by exact name, else first.
                match = emailMatches.FirstOrDefault(d =>
                    string.Equals(d.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(d.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                        ?? emailMatches[0];
            }

            // 2) If no email match and phone present, try match by phone.
            if (match == null && !string.IsNullOrWhiteSpace(phone))
            {
                if (donorByPhone.TryGetValue(phone, out var phoneMatches) && phoneMatches.Count > 0)
                {
                    // If multiple donors share the same phone, pick by exact name, else first.
                    match = phoneMatches.FirstOrDefault(d =>
                        string.Equals(d.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                            ?? phoneMatches[0];
                }
            }

            if (match != null)
            {
                return (match, false);
            }

            // 3) No match => create NEW donor.
            //    Because we keyed only by email/phone, same name + different email will come here
            //    and create a separate donor record, as required.
            var donor = new DonorMaster
            {
                FirstName = firstName,
                LastName = lastName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                IsOrganization = false,
                DonorType = "Individual",
                AllowEmail = true,
                AllowSms = false,
                AllowMail = true,
                Notes = "Created via bulk upload",
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedBy = "bulk-import",
                IsActive = true
            };

            return (donor, true);
        }

        private static DonorMaster? ResolveByName(
            List<DonorMaster> candidates,
            string firstName,
            string lastName)
        {
            if (candidates.Count == 1)
                return candidates[0];

            var exact = candidates.FirstOrDefault(d =>
                string.Equals(d.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(d.LastName, lastName, StringComparison.OrdinalIgnoreCase));

            return exact ?? candidates[0];
        }
    }
}
