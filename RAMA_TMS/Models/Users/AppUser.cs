namespace RAMA_TMS.Models.Users
{
    public class AppUser
    {
        public long Id { get; set; }

        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;

        // "Admin", "Collector", "Viewer"
        public string Role { get; set; } = "Collector";

        public bool IsActive { get; set; } = true;
        public string? PasswordHash { get; set; }  // for local accounts
        public string? PasswordSalt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "system";

        public ICollection<DonorReceiptDetail> CollectedDonations { get; set; } = new List<DonorReceiptDetail>();
    }
}
