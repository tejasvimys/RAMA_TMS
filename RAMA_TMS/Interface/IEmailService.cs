namespace RAMA_TMS.Interface
{
    public interface IEmailService
    {
        Task SendReceiptAsync(string toEmail, string subject, string bodyText, byte[] pdfBytes);
    }
}
