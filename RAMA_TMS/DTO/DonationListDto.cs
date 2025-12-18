namespace RAMA_TMS.DTO
{
    public class DonationListDto
    {
        public long DonorReceiptDetailId { get; set; }
        public long DonorId { get; set; }
        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public DateTimeOffset DateOfDonation { get; set; }  // or DateTimeOffset
        public string? PaymentMode { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }

        public string DonorFirstName { get; set; } = null!;
        public string DonorLastName { get; set; } = null!;
        public string? DonorEmail { get; set; }
    }
}
