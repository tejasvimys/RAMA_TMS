namespace RAMA_TMS.Models
{
    public class DonorMaster
    {
        public long DonorId { get; set; }

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
        public string? TaxId { get; set; }
        public string? DonorType { get; set; }

        public string? PreferredContactMethod { get; set; }
        public bool AllowEmail { get; set; } = true;
        public bool AllowSms { get; set; } = false;
        public bool AllowMail { get; set; } = true;
        public string? Notes { get; set; }

        public string? CreatedBy { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? UpdateDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<DonorReceiptDetail> ReceiptDetails { get; set; } = new List<DonorReceiptDetail>();
    }

}
