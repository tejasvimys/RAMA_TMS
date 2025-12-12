namespace RAMA_TMS.DTO
{
    public class DonorReceiptDTO
    {
        public long DonorId { get; set; }

        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public string Currency { get; set; } = "USD";
        public DateTimeOffset? DateOfDonation { get; set; }

        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }

        public bool IsTaxDeductible { get; set; } = true;
        public bool IsAnonymous { get; set; } = false;

        public string? InternalNotes { get; set; }
    }
}
