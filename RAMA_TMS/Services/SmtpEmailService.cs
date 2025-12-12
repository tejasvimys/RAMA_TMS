using Microsoft.Extensions.Options;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using System.Net;
using System.Net.Mail;

namespace RAMA_TMS.Services
{
    public class SmtpEmailService: IEmailService
    {
        private readonly SmtpEmailSettings _settings;

        public SmtpEmailService(IOptions<SmtpEmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendReceiptAsync(string toEmail, string subject, string bodyText, byte[] pdfBytes)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = bodyText;
            message.IsBodyHtml = false;

            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                var pdfStream = new System.IO.MemoryStream(pdfBytes);
                var attachment = new Attachment(pdfStream, "DonationReceipt.pdf", "application/pdf");
                message.Attachments.Add(attachment);
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
