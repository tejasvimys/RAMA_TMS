namespace RAMA_TMS.Models.Import
{
    public class DonationImportRowResult
    {
        // Original CSV fields (for failed row export)
        public string? FullName { get; set; }
        public string? DonationAmountRaw { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DateRaw { get; set; }

        // Error info (null when successfully imported)
        public string? ErrorMessage { get; set; }
    }

    public class DonationImportFailedRowDto
    {
        public string? FullName { get; set; }
        public string? DonationAmountRaw { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DateRaw { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DonationImportImportedRowDto
    {
        public long DonorId { get; set; }
        public string? DonorName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal DonationAmount { get; set; }
        public DateTimeOffset DateOfDonation { get; set; }
        public string Status { get; set; } = "Imported";
    }

    public class DonationImportSummary
    {
        public int DonorsCreated { get; set; }
        public int DonorsMatched { get; set; }
        public int DonationsImported { get; set; }
        public int RowsFailed { get; set; }

        public List<DonationImportRowResult> FailedRows { get; set; } = new();

        // API-facing DTO list
        public List<DonationImportFailedRowDto> FailedRowDtos =>
            FailedRows.Select(r => new DonationImportFailedRowDto
            {
                FullName = r.FullName,
                DonationAmountRaw = r.DonationAmountRaw,
                Email = r.Email,
                Phone = r.Phone,
                DateRaw = r.DateRaw,
                ErrorMessage = r.ErrorMessage
            }).ToList();

        public List<DonationImportImportedRowDto> ImportedRows { get; set; } = new();
    }
}
