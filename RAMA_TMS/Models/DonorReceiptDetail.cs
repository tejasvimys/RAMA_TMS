using RAMA_TMS.Models.Users;

namespace RAMA_TMS.Models
{
    public class DonorReceiptDetail
    {
        public long DonorReceiptDetailId { get; set; }

        public long DonorId { get; set; }

        public decimal DonationAmt { get; set; }
        public string DonationType { get; set; } = null!;
        public string Currency { get; set; } = "USD";
        public DateTimeOffset DateOfDonation { get; set; }

        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public bool IsTaxDeductible { get; set; } = true;
        public bool IsAnonymous { get; set; } = false;
        public string? InternalNotes { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? UpdateDate { get; set; }
        public bool IsActive { get; set; } = true;

        //public string? Notes { get; set; } 

        public DonorMaster Donor { get; set; } = null!;

        public long? CollectedByUserId { get; set; }   // FK to AppUser

        public AppUser? CollectedByUser { get; set; }
    }
}
