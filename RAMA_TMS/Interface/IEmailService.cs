using RAMA_TMS.DTO;

namespace RAMA_TMS.Interface
{
    public interface IEmailService
    {
        Task SendReceiptAsync(string toEmail, string subject, string bodyText, byte[] pdfBytes);

        Task SendEndOfDayReportAsync(
            List<string> recipientEmails,
            EndOfDayReportDto report,
            DateTime reportDate,
            byte[] pdfAttachment);
    }
}
