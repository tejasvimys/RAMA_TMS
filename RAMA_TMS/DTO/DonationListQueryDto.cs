namespace RAMA_TMS.DTO
{
    public class DonationListQueryDto
    {
        public int Year { get; set; }          // required

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public string? Search { get; set; }

        public string? Sort { get; set; }      // "Date", "Amount", "Donor", "ReceiptId"

        public string? Dir { get; set; }       // "asc" or "desc"
    }
}
