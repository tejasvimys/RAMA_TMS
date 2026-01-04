namespace RAMA_TMS.Models.Users
{
    public class AppUser
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Role { get; set; } = "Collector";
        public bool IsActive { get; set; } = true;
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }

        // 2FA fields
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public List<string>? BackupCodes { get; set; }

        // Password Reset fields (NEW)
        public string? PasswordResetToken { get; set; }
        public DateTimeOffset? PasswordResetTokenExpiry { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "system";

        public ICollection<DonorReceiptDetail> CollectedDonations { get; set; } = new List<DonorReceiptDetail>();
    }

}
