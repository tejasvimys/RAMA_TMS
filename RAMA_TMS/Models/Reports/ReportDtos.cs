namespace RAMA_TMS.Models.Reports
{
    public class EndOfDayReportDto
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public double TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public int UniqueDonors { get; set; }
        public double AverageDonation { get; set; }
        public List<DonationTypeBreakdownDto> ByDonationType { get; set; }
        public List<PaymentMethodBreakdownDto> ByPaymentMethod { get; set; }
        public List<DonationDetailDto> Donations { get; set; }
        public string CollectorName { get; set; }
        public string CollectorEmail { get; set; }
    }

    public class DonationTypeBreakdownDto
    {
        public string Type { get; set; }
        public double Amount { get; set; }
        public int Count { get; set; }
    }

    public class PaymentMethodBreakdownDto
    {
        public string Type { get; set; }
        public double Amount { get; set; }
        public int Count { get; set; }
    }

    public class DonationDetailDto
    {
        public string Id { get; set; }
        public string DonorName { get; set; }
        public double Amount { get; set; }
        public string DonationType { get; set; }
        public string PaymentMode { get; set; }
        public string ReferenceNo { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Notes { get; set; }
    }
}
