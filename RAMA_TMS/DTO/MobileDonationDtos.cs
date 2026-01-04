namespace RAMA_TMS.DTO
{
    public class MobileDonationDtos
    {
    }

    public class MobileDonationListItem
    {
        public long DonorReceiptDetailId { get; set; }
        public long DonorId { get; set; }
        public string DonorName { get; set; } = null!;
        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public DateTimeOffset DateOfDonation { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }
    }

    // ✅ NEW: Mobile donation response with PDF access
    public class MobileQuickDonationResponse
    {
        public long DonorId { get; set; }
        public long DonorReceiptDetailId { get; set; }
        public string DonorFullName { get; set; } = string.Empty;
        public decimal DonationAmt { get; set; }
        public DateTimeOffset DateOfDonation { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string DonationType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public bool EmailSent { get; set; }

        // ✅ URL to download PDF
        public string ReceiptPdfUrl { get; set; } = string.Empty;
    }
}
