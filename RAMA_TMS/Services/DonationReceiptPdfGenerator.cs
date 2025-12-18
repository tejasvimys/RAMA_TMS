using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using System;
using System.Globalization;

namespace RAMA_TMS.Services
{
    public class DonationReceiptPdfGenerator : IDonationReceiptPdfGenerator
    {

        private readonly string _logoImagePath;

        public DonationReceiptPdfGenerator(string logoImagePath)
        {
            _logoImagePath = logoImagePath; // e.g. "wwwroot/images/rama-logo.png"
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateReceipt(DonorReceiptDetail receipt, DonorMaster donor)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            if (donor == null) throw new ArgumentNullException(nameof(donor));

            var doc = new RamaReceiptDocument(receipt, donor, _logoImagePath);
            return doc.GeneratePdf();
        }

        private class RamaReceiptDocument : IDocument
        {
            private readonly DonorReceiptDetail _receipt;
            private readonly DonorMaster _donor;
            private readonly string _logoImagePath;

            public RamaReceiptDocument(DonorReceiptDetail receipt, DonorMaster donor, string logoImagePath)
            {
                _receipt = receipt;
                _donor = donor;
                _logoImagePath = logoImagePath;
            }

            public DocumentMetadata GetMetadata() => new DocumentMetadata
            {
                Title = "Donation Receipt",
                Author = "Ananthaadi Rayara Matha (RAMA), Atlanta"
            };

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Content().Column(col =>
                    {
                        ComposeHeader(col);
                        ComposeGreeting(col);
                        ComposeBody(col);
                        ComposeThanks(col);
                        ComposeFooter(col);
                    });
                });
            }

            private void ComposeHeader(ColumnDescriptor col)
            {
                // All text centered at top
                col.Item().AlignCenter().Column(c =>
                {
                    c.Item().Text("|| SRI MOOLARAMO VIJAYATE || SRI GURURAJO VIJAYATE ||")
                        .Bold().FontSize(8).AlignCenter();
                    c.Item().Text("HARI SARVOTTAMA VAAYU JEEVOTTAMA")
                        .Bold().FontSize(10).AlignCenter();
                    c.Item().Text("ANANTHAADI RAYARA MATHA (RAMA), ATLANTA")
                        .Bold().Underline().FontSize(18).AlignCenter();
                    c.Item().Text("TAX ID: 47-4926883")
                        .FontSize(12).AlignCenter();
                });

                col.Item().Height(10);

                // Logo full-width and centered, no fixed height constraint
                if (!string.IsNullOrWhiteSpace(_logoImagePath))
                {
                    try
                    {
                        col.Item().AlignCenter().Element(e =>
                e.Image(_logoImagePath)
                 .FitWidth());
                    }
                    catch
                    {
                        // If logo is missing or invalid, skip rendering it
                    }
                }

                col.Item().Height(20);
            }

            private void ComposeGreeting(ColumnDescriptor col)
            {
                col.Item().Text("Hare Srinivasa!!!")
                    .Bold()
                    .FontSize(11);

                col.Item().Height(10);

                var first = (_donor.FirstName ?? "").Trim();
                var last = (_donor.LastName ?? "").Trim();
                var donorFullName = $"{first} {last}".Trim();

                if (string.IsNullOrWhiteSpace(donorFullName))
                {
                    donorFullName = string.IsNullOrWhiteSpace(_donor.Email)
                        ? "Devotee"
                        : _donor.Email;
                }

                col.Item().Text($"Dear {donorFullName},")
                    .FontSize(11);

                col.Item().Height(10);
            }

            private void ComposeBody(ColumnDescriptor col)
            {
                // Use local or UTC year depending on how DateOfDonation is stored
                var year = (_receipt.DateOfDonation as DateTimeOffset?)?.Year
                           ?? _receipt.DateOfDonation.Year;
                var amountText = _receipt.DonationAmt.ToString(
                    "C", CultureInfo.CreateSpecificCulture("en-US"));

                col.Item().Text(
                    $"On behalf of Ananthaadi Rayara Matha (RAMA), Atlanta Inc, we would like to thank you for your generous donation of {amountText} for the year {year}.")
                    .FontSize(11);

                col.Item().Height(10);

                col.Item().Text(
                    "Ananthaadi Rayara Matha Atlanta,Inc is a 501 ( c ) (3) registered non-profit, charitable organization and contributions to the matha are tax deductible to the extent allowed by law. No services were provided to you in return for the donation. Once again, thanks for your continued support for RAMA.")
                    .FontSize(11);

                col.Item().Height(10);

                col.Item().Text(
                    "Donations received in good faith are not refundable.")
                    .FontSize(11);

                col.Item().Height(10);

                col.Item().Text(
                    "For credit card donations, kindly note that the receipts are provided after deducting the transaction fee that was charged by the provider.")
                    .FontSize(11);

                col.Item().Height(15);
            }

            private void ComposeThanks(ColumnDescriptor col)
            {
                col.Item().Text("Thanks,")
                    .FontSize(11);

                col.Item().Text("RAMA")
                    .Bold()
                    .FontSize(11);

                col.Item().Height(25);
            }

            private void ComposeFooter(ColumnDescriptor col)
            {
                col.Item().AlignCenter().Text(
                    "|| pUjyAya Raghavendraya Sathya Dharma Rathayacha | Bajatham Kalpa Vrukshaya Namatham Kamadehnave ||")
                    .FontSize(10);
            }
        }
        //    private readonly string _logoImagePath;

        //    public DonationReceiptPdfGenerator(string logoImagePath)
        //    {
        //        _logoImagePath = logoImagePath; // e.g. "wwwroot/images/rama-logo.png"
        //        QuestPDF.Settings.License = LicenseType.Community;
        //    }

        //    public byte[] GenerateReceipt(DonorReceiptDetail receipt, DonorMaster donor)
        //    {
        //        if (receipt == null) throw new ArgumentNullException(nameof(receipt));
        //        if (donor == null) throw new ArgumentNullException(nameof(donor));

        //        var doc = new RamaReceiptDocument(receipt, donor, _logoImagePath);
        //        return doc.GeneratePdf();
        //    }

        //    private class RamaReceiptDocument : IDocument
        //    {
        //        private readonly DonorReceiptDetail _receipt;
        //        private readonly DonorMaster _donor;
        //        private readonly string _logoImagePath;

        //        public RamaReceiptDocument(DonorReceiptDetail receipt, DonorMaster donor, string logoImagePath)
        //        {
        //            _receipt = receipt;
        //            _donor = donor;
        //            _logoImagePath = logoImagePath;
        //        }

        //        public DocumentMetadata GetMetadata() => new DocumentMetadata
        //        {
        //            Title = "Donation Receipt",
        //            Author = "Ananthaadi Rayara Matha (RAMA), Atlanta"
        //        };

        //        public void Compose(IDocumentContainer container)
        //        {
        //            container.Page(page =>
        //            {
        //                page.Size(PageSizes.A4);
        //                page.Margin(40);
        //                page.DefaultTextStyle(x => x.FontSize(11));
        //                page.Content().Column(col =>
        //                {
        //                    ComposeHeader(col);
        //                    ComposeGreeting(col);
        //                    ComposeBody(col);
        //                    ComposeThanks(col);
        //                    ComposeFooter(col);
        //                });
        //            });
        //        }

        //        private void ComposeHeader(ColumnDescriptor col)
        //        {
        //            col.Item().Row(row =>
        //            {
        //                // Logo left (if present)
        //                if (!string.IsNullOrWhiteSpace(_logoImagePath))
        //                {
        //                    row.RelativeColumn(1).AlignLeft().Column(c =>
        //                    {
        //                        try
        //                        {
        //                            c.Item().Height(70).Image(_logoImagePath)
        //                                .FitHeight(); // or .FitWidth() depending on how your logo looks
        //                        }
        //                        catch
        //                        {
        //                            // If logo is missing or invalid, just skip rendering it
        //                        }
        //                    });
        //                }

        //                // Centered text
        //                row.RelativeColumn(3).AlignCenter().Column(c =>
        //                {
        //                    c.Item().Text("|| SRI MOOLARAMO VIJAYATE || SRI GURURAJO VIJAYATE ||")
        //                        .Bold().FontSize(8);
        //                    c.Item().Text("HARI SARVOTTAMA VAAYU JEEVOTTAMA")
        //                        .Bold().FontSize(10);
        //                    c.Item().Text("ANANTHAADI RAYARA MATHA (RAMA), ATLANTA")
        //                        .Bold().Underline().FontSize(14);
        //                    c.Item().Text("TAX ID: 47-4926883")
        //                        .FontSize(10);
        //                });
        //            });

        //            col.Item().Height(15);
        //        }

        //        private void ComposeGreeting(ColumnDescriptor col)
        //        {
        //            col.Item().Text("Hare Srinivasa!!!")
        //                .Bold()
        //                .FontSize(11);

        //            col.Item().Height(10);

        //            var first = (_donor.FirstName ?? "").Trim();
        //            var last = (_donor.LastName ?? "").Trim();
        //            var donorFullName = $"{first} {last}".Trim();

        //            if (string.IsNullOrWhiteSpace(donorFullName))
        //            {
        //                donorFullName = string.IsNullOrWhiteSpace(_donor.Email)
        //                    ? "Devotee"
        //                    : _donor.Email;
        //            }

        //            col.Item().Text($"Dear {donorFullName},")
        //.FontSize(11);

        //            col.Item().Height(10);
        //        }

        //        private void ComposeBody(ColumnDescriptor col)
        //        {
        //            var year = _receipt.DateOfDonation.Year;
        //            var amountText = _receipt.DonationAmt.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));

        //            col.Item().Text(
        //                $"On behalf of Ananthaadi Rayara Matha (RAMA), Atlanta Inc, we would like to thank you for your generous donation of {amountText} for the year {year}.")
        //                .FontSize(11);

        //            col.Item().Height(10);

        //            col.Item().Text(
        //                "Ananthaadi Rayara Matha Atlanta,Inc is a 501 ( c ) (3) registered non-profit, charitable organization and contributions to the temple are tax deductible to the extent allowed by law. No services were provided to you in return for the donation. Once again, thanks for your continued support for RAMA.")
        //                .FontSize(11);

        //            col.Item().Height(10);

        //            col.Item().Text(
        //                "For credit card donations, kindly note that the receipts are provided after deducting the transaction fee that was charged by the provider.")
        //                .FontSize(11);

        //            col.Item().Height(15);
        //        }

        //        private void ComposeThanks(ColumnDescriptor col)
        //        {
        //            col.Item().Text("Thanks,")
        //                .FontSize(11);

        //            col.Item().Text("RAMA")
        //                .Bold()
        //                .FontSize(11);

        //            col.Item().Height(25);
        //        }

        //        private void ComposeFooter(ColumnDescriptor col)
        //        {
        //            col.Item().AlignCenter().Text(
        //                "|| pUjyAya Raghavendraya Sathya Dharma Rathayacha | Bajatham Kalpa Vrukshaya Namatham Kamadehnave ||")
        //                .FontSize(10);
        //        }
        //    }
    }
}
