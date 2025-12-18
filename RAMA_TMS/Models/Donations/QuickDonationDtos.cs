namespace RAMA_TMS.Models.Donations
{
    // Models/Donations/QuickDonationDtos.cs
    public class QuickDonorDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public bool IsOrganization { get; set; }
        public string? OrganizationName { get; set; }
        public string DonorType { get; set; } = "Individual"; // reuse your constant
    }

    public class QuickDonationDto
    {
        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public DateTime DateOfDonation { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNo { get; set; }
        //public string? Notes { get; set; }
    }

    public class QuickDonorAndDonationRequest
    {
        public QuickDonorDto Donor { get; set; } = null!;
        public QuickDonationDto Donation { get; set; } = null!;
    }

    public class QuickDonationResponse
    {
        public long DonorId { get; set; }
        public long DonorReceiptDetailId { get; set; }
        public string DonorFullName { get; set; } = null!;
        public decimal DonationAmt { get; set; }
        public DateTimeOffset DateOfDonation { get; set; }
    }

}
