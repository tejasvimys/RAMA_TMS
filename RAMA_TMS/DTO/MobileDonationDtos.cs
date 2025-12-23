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
}
