namespace RAMA_TMS.Models.Donations
{
    public class MobileDonationRequest
    {
        public long DonorId { get; set; }   // existing donor; you can extend to create donor too
        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public DateTimeOffset DateOfDonation { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }
    }

    public class MobileDonationResponse
    {
        public long DonorReceiptDetailId { get; set; }
        public long DonorId { get; set; }
        public decimal DonationAmt { get; set; }
        public DateTimeOffset DateOfDonation { get; set; }
    }

    public class DaySummaryDto
    {
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }
}
