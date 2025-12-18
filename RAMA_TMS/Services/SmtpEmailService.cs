using Microsoft.Extensions.Options;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
//using System.Net;
//using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                TextBody = bodyText
            };

            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                builder.Attachments.Add(
                    "DonationReceipt.pdf",
                    pdfBytes,
                    new ContentType("application", "pdf"));
            }

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            // Choose security based on settings
            var secureOption = _settings.EnableSsl
                ? SecureSocketOptions.StartTls           // Gmail / real SMTP
                : SecureSocketOptions.None;              // Mailpit: no TLS, no STARTTLS

            await smtp.ConnectAsync(_settings.Server, _settings.Port, secureOption);

            if (!string.IsNullOrEmpty(_settings.UserName))
            {
                await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        //if (string.IsNullOrWhiteSpace(toEmail))
        //    throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        //using var message = new MailMessage();
        //message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
        //message.To.Add(new MailAddress(toEmail));
        //message.Subject = subject;
        //message.Body = bodyText;
        //message.IsBodyHtml = false;

        //if (pdfBytes != null && pdfBytes.Length > 0)
        //{
        //    var pdfStream = new System.IO.MemoryStream(pdfBytes);
        //    var attachment = new Attachment(pdfStream, "DonationReceipt.pdf", "application/pdf");
        //    message.Attachments.Add(attachment);
        //}

        ////using var client = new SmtpClient(_settings.Host, _settings.Port)
        ////{
        ////    EnableSsl = _settings.EnableSsl,
        ////    Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
        ////};

        //await client.SendMailAsync(message);
    }
    
}
