namespace RAMA_TMS.DTO
{
    public class DonationListItemDto
    {
        public long DonorReceiptDetailId { get; set; }

        public long DonorId { get; set; }

        public DateTime DateOfDonation { get; set; }

        public decimal DonationAmt { get; set; }

        public string DonationType { get; set; } = string.Empty;

        public string? PaymentMode { get; set; }

        public string? ReferenceNo { get; set; }

        public string? Notes { get; set; }

        public string DonorFirstName { get; set; } = string.Empty;

        public string DonorLastName { get; set; } = string.Empty;

        public string? DonorEmail { get; set; }
    }
}
