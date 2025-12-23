//using System.Net;
//using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using RAMA_TMS.DTO;

namespace RAMA_TMS.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpEmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpEmailSettings> options, ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
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
            var secureOption = _settings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await smtp.ConnectAsync(_settings.Server, _settings.Port, secureOption);

            if (!string.IsNullOrEmpty(_settings.UserName))
            {
                await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendEndOfDayReportAsync(
            List<string> recipientEmails,
            EndOfDayReportDto report,
            DateTime reportDate,
            byte[] pdfAttachment)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

                // Add all recipient emails
                foreach (var email in recipientEmails)
                {
                    message.To.Add(MailboxAddress.Parse(email));
                }

                message.Subject = $"End of Day Report - {reportDate:MMMM dd, yyyy}";

                var builder = new BodyBuilder
                {
                    HtmlBody = ComposeEndOfDayEmailBody(report, reportDate)
                };

                // Attach PDF
                if (pdfAttachment != null && pdfAttachment.Length > 0)
                {
                    builder.Attachments.Add(
                        $"EOD_Report_{reportDate:yyyy-MM-dd}.pdf",
                        pdfAttachment,
                        new ContentType("application", "pdf"));
                }

                message.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                var secureOption = _settings.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await smtp.ConnectAsync(_settings.Server, _settings.Port, secureOption);

                if (!string.IsNullOrEmpty(_settings.UserName))
                {
                    await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
                }

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("End of day report sent successfully to {Count} recipients for date {Date}",
                    recipientEmails.Count, reportDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send end of day report email for date {Date}", reportDate);
                throw;
            }
        }

        private string ComposeEndOfDayEmailBody(EndOfDayReportDto report, DateTime reportDate)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 16px;
            opacity: 0.9;
        }}
        .content {{
            padding: 30px;
        }}
        .summary-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin: 20px 0;
        }}
        .summary-card {{
            background: #f8f9fa;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            border-left: 4px solid #667eea;
        }}
        .summary-card .label {{
            font-size: 12px;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }}
        .summary-card .value {{
            font-size: 28px;
            font-weight: bold;
            color: #667eea;
            margin: 0;
        }}
        .section {{
            margin: 30px 0;
        }}
        .section-title {{
            font-size: 18px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e9ecef;
        }}
        .breakdown-item {{
            display: flex;
            justify-content: space-between;
            padding: 12px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        .breakdown-item:last-child {{
            border-bottom: none;
        }}
        .breakdown-label {{
            font-weight: 500;
            color: #495057;
        }}
        .breakdown-value {{
            color: #28a745;
            font-weight: bold;
        }}
        .breakdown-count {{
            color: #6c757d;
            font-size: 14px;
            margin-left: 10px;
        }}
        .attachment-notice {{
            background: #e7f3ff;
            border-left: 4px solid #2196F3;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
            text-align: center;
        }}
        .attachment-notice .icon {{
            font-size: 32px;
            margin-bottom: 10px;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            background: #f8f9fa;
            color: #6c757d;
            font-size: 12px;
        }}
        .footer p {{
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📊 End of Day Report</h1>
            <p>{reportDate:dddd, MMMM dd, yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='summary-grid'>
                <div class='summary-card'>
                    <div class='label'>Total Amount</div>
                    <div class='value'>${report.TotalAmount:N2}</div>
                </div>
                <div class='summary-card'>
                    <div class='label'>Total Donations</div>
                    <div class='value'>{report.TotalCount}</div>
                </div>
                <div class='summary-card'>
                    <div class='label'>Unique Donors</div>
                    <div class='value'>{report.UniqueDonors}</div>
                </div>
                <div class='summary-card'>
                    <div class='label'>Average Donation</div>
                    <div class='value'>${report.AverageDonation:N2}</div>
                </div>
            </div>

            <div class='section'>
                <div class='section-title'>💰 Donation Type Breakdown</div>
                {string.Join("", report.ByDonationType.Select(b => $@"
                <div class='breakdown-item'>
                    <span class='breakdown-label'>{b.Type}</span>
                    <span>
                        <span class='breakdown-value'>${b.Amount:N2}</span>
                        <span class='breakdown-count'>({b.Count} donations)</span>
                    </span>
                </div>
                "))}
            </div>

            <div class='section'>
                <div class='section-title'>💳 Payment Method Breakdown</div>
                {string.Join("", report.ByPaymentMethod.Select(b => $@"
                <div class='breakdown-item'>
                    <span class='breakdown-label'>{b.Type}</span>
                    <span>
                        <span class='breakdown-value'>${b.Amount:N2}</span>
                        <span class='breakdown-count'>({b.Count} transactions)</span>
                    </span>
                </div>
                "))}
            </div>

            <div class='attachment-notice'>
                <div class='icon'>📎</div>
                <p style='margin: 0; color: #0066cc; font-weight: 500;'>
                    Detailed PDF report is attached to this email
                </p>
            </div>

            {(!string.IsNullOrEmpty(report.CollectorName) ? $@"
            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #e9ecef;'>
                <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                    <strong>Collected by:</strong> {report.CollectorName}
                    {(!string.IsNullOrEmpty(report.CollectorEmail) ? $" ({report.CollectorEmail})" : "")}
                </p>
            </div>
            " : "")}
        </div>

        <div class='footer'>
            <p><strong>RAMA Temple Management System</strong></p>
            <p>This is an automated report</p>
            <p>Generated on {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}</p>
        </div>
    </div>
</body>
</html>";
        }


        //public async Task SendReceiptAsync(string toEmail, string subject, string bodyText, byte[] pdfBytes)
        //{
        //    var message = new MimeMessage();
        //    message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        //    message.To.Add(MailboxAddress.Parse(toEmail));
        //    message.Subject = subject;

        //    var builder = new BodyBuilder
        //    {
        //        TextBody = bodyText
        //    };

        //    if (pdfBytes != null && pdfBytes.Length > 0)
        //    {
        //        builder.Attachments.Add(
        //            "DonationReceipt.pdf",
        //            pdfBytes,
        //            new ContentType("application", "pdf"));
        //    }

        //    message.Body = builder.ToMessageBody();

        //    using var smtp = new SmtpClient();
        //    // Choose security based on settings
        //    var secureOption = _settings.EnableSsl
        //        ? SecureSocketOptions.StartTls           // Gmail / real SMTP
        //        : SecureSocketOptions.None;              // Mailpit: no TLS, no STARTTLS

        //    await smtp.ConnectAsync(_settings.Server, _settings.Port, secureOption);

        //    if (!string.IsNullOrEmpty(_settings.UserName))
        //    {
        //        await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
        //    }

        //    await smtp.SendAsync(message);
        //    await smtp.DisconnectAsync(true);
        //}

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
