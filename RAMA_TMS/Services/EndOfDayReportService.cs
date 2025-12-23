using Google;
using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.Interface;
using RAMA_TMS.DTO;

namespace RAMA_TMS.Services
{
    public class EndOfDayReportService : IEndOfDayReportService
    {
        private readonly TMSDBContext _context;
        private readonly ILogger<EndOfDayReportService> _logger;

        public EndOfDayReportService(TMSDBContext context, ILogger<EndOfDayReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EndOfDayReportDto> GetReportAsync(DateTime date, int userId, string userRole)
        {
            try
            {
                _logger.LogInformation("Generating report for date: {Date}, user: {UserId}, role: {Role}",
                    date, userId, userRole);

                // Convert to UTC for PostgreSQL
                var startOfDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
                var endOfDay = DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                _logger.LogInformation("Date range (UTC): {Start} to {End}", startOfDay, endOfDay);

                // Build query based on user role
                var query = _context.DonorReceiptDetails
                    .Include(d => d.Donor)
                    .Include(d => d.CollectedByUser)
                    .Where(d => d.DateOfDonation >= startOfDay && d.DateOfDonation <= endOfDay);

                // If user is Collector, filter by their ID only
                if (userRole != null && userRole.Equals("Collector", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.CollectedByUserId == userId);
                    _logger.LogInformation("Filtering donations for Collector with ID: {UserId}", userId);
                }
                else if (userRole != null && userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Admin user - showing all donations");
                }
                else
                {
                    _logger.LogWarning("Unknown role: {Role}, defaulting to collector view", userRole);
                    query = query.Where(d => d.CollectedByUserId == userId);
                }

                var donations = await query
                    .OrderBy(d => d.DateOfDonation)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} donations for user {UserId}", donations.Count, userId);

                if (!donations.Any())
                {
                    _logger.LogWarning("No donations found for date {Date}, user {UserId}", date, userId);
                    return null;
                }

                var totalAmount = donations.Sum(d => d.DonationAmt);
                var totalCount = donations.Count;
                var uniqueDonors = donations.Select(d => d.DonorId).Distinct().Count();
                var averageDonation = totalAmount / totalCount;

                _logger.LogInformation("Total: {Total}, Count: {Count}, Unique: {Unique}",
                    totalAmount, totalCount, uniqueDonors);

                // Group by donation type
                var byDonationType = donations
                    .GroupBy(d => d.DonationType ?? "General")
                    .Select(g => new DonationTypeBreakdownDto
                    {
                        Type = g.Key,
                        Amount = (double)g.Sum(d => d.DonationAmt),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                // Group by payment method
                var byPaymentMethod = donations
                    .GroupBy(d => d.PaymentMethod ?? "Not Specified")
                    .Select(g => new PaymentMethodBreakdownDto
                    {
                        Type = g.Key,
                        Amount = (double)g.Sum(d => d.DonationAmt),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                // Donation details
                var donationDetails = donations
                    .Select(d => new DonationDetailDto
                    {
                        Id = d.DonorReceiptDetailId.ToString(),
                        DonorName = GetDonorName(d),
                        Amount = (double)d.DonationAmt,
                        DonationType = d.DonationType ?? "General",
                        PaymentMode = d.PaymentMethod ?? "Not Specified",
                        ReferenceNo = d.PaymentReference,
                        Timestamp = d.DateOfDonation,
                        Notes = d.InternalNotes
                    })
                    .ToList();

                // Get collector information
                var firstDonation = donations.FirstOrDefault();
                var collectorName = firstDonation?.CollectedByUser?.DisplayName;
                var collectorEmail = firstDonation?.CollectedByUser?.Email;

                var report = new EndOfDayReportDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = date,
                    TotalAmount = (double)totalAmount,
                    TotalCount = totalCount,
                    UniqueDonors = uniqueDonors,
                    AverageDonation = (double)averageDonation,
                    ByDonationType = byDonationType,
                    ByPaymentMethod = byPaymentMethod,
                    Donations = donationDetails,
                    CollectorName = collectorName,
                    CollectorEmail = collectorEmail
                };

                _logger.LogInformation("Report generated successfully");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report for date {Date}, user {UserId}", date, userId);
                throw;
            }
        }

        public async Task<List<string>> GetAdminEmailsAsync()
        {
            try
            {
                var adminEmails = await _context.AppUsers
                    .Where(u => u.Role == "Admin" && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} admin email addresses", adminEmails.Count);
                return adminEmails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin emails");
                throw;
            }
        }

        private string GetDonorName(Models.DonorReceiptDetail donation)
        {
            try
            {
                if (donation.Donor == null)
                {
                    _logger.LogWarning("Donor is null for donation {Id}", donation.DonorReceiptDetailId);
                    return "Unknown Donor";
                }

                if (donation.Donor.IsOrganization && !string.IsNullOrEmpty(donation.Donor.OrganizationName))
                {
                    return donation.Donor.OrganizationName;
                }

                var firstName = donation.Donor.FirstName ?? "";
                var lastName = donation.Donor.LastName ?? "";
                var fullName = $"{firstName} {lastName}".Trim();

                return string.IsNullOrEmpty(fullName) ? "Unknown Donor" : fullName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting donor name for donation {Id}", donation.DonorReceiptDetailId);
                return "Unknown Donor";
            }
        }
    }
}

