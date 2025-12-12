using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Models;

namespace RAMA_TMS.Data
{
    public class TMSDBContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public TMSDBContext(Microsoft.EntityFrameworkCore.DbContextOptions<TMSDBContext> options)
            : base(options)
        {
        }
        public Microsoft.EntityFrameworkCore.DbSet<Models.DonorMaster> DonorMasters { get; set; } = null!;
        public Microsoft.EntityFrameworkCore.DbSet<Models.DonorReceiptDetail> DonorReceiptDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureDonorMaster(modelBuilder);
            ConfigureDonorReceiptDetail(modelBuilder);
        }

        private static void ConfigureDonorMaster(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DonorMaster>();

            entity.ToTable("DonorMaster"); // public schema by default

            entity.HasKey(x => x.DonorId);

            entity.Property(x => x.DonorId).ValueGeneratedOnAdd();

            entity.Property(x => x.FirstName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.LastName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.Phone).HasMaxLength(25);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Address1).HasMaxLength(255);
            entity.Property(x => x.Address2).HasMaxLength(255);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(100);
            entity.Property(x => x.Country).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(20);

            entity.Property(x => x.OrganizationName).HasMaxLength(255);
            entity.Property(x => x.TaxId).HasMaxLength(50);
            entity.Property(x => x.DonorType).HasMaxLength(50);

            entity.Property(x => x.PreferredContactMethod).HasMaxLength(20);

            entity.Property(x => x.AllowEmail).HasDefaultValue(true);
            entity.Property(x => x.AllowSms).HasDefaultValue(false);
            entity.Property(x => x.AllowMail).HasDefaultValue(true);

            entity.Property(x => x.CreatedDate)
                  .HasDefaultValueSql("NOW()");

            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.UpdatedBy).HasMaxLength(100);

            entity.HasMany(x => x.ReceiptDetails)
                  .WithOne(x => x.Donor)
                  .HasForeignKey(x => x.DonorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.IsActive)
      .IsRequired()
      .HasDefaultValue(true);

            entity.HasQueryFilter(x => x.IsActive);

        }

        private static void ConfigureDonorReceiptDetail(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DonorReceiptDetail>();

            entity.ToTable("DonorReceiptDetail");

            entity.HasKey(x => x.DonorReceiptDetailId);

            entity.Property(x => x.DonorReceiptDetailId)
                  .ValueGeneratedOnAdd();

            entity.Property(x => x.DonationAmt)
                  .HasColumnType("numeric(12,2)")
                  .IsRequired();

            entity.Property(x => x.DonationType)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(x => x.Currency)
                  .HasMaxLength(10)
                  .HasDefaultValue("USD");

            entity.Property(x => x.DateOfDonation)
                  .IsRequired();

            entity.Property(x => x.PaymentMethod)
                  .HasMaxLength(20);

            entity.Property(x => x.PaymentReference)
                  .HasMaxLength(100);

            entity.Property(x => x.IsTaxDeductible)
                  .HasDefaultValue(true);

            entity.Property(x => x.IsAnonymous)
                  .HasDefaultValue(false);

            entity.Property(x => x.CreatedDate)
                  .HasDefaultValueSql("NOW()");

            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.Property(x => x.UpdatedBy).HasMaxLength(100);

            entity.HasOne(x => x.Donor)
                  .WithMany(x => x.ReceiptDetails)
                  .HasForeignKey(x => x.DonorId)
                  .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
