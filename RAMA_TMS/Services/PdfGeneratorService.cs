using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RAMA_TMS.Interface;
using RAMA_TMS.DTO;

namespace RAMA_TMS.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        public async Task<byte[]> GenerateEndOfDayReportPdfAsync(EndOfDayReportDto report, DateTime date)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(40);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        page.Header().Element(ComposeHeader);
                        page.Content().Element(container => ComposeContent(container, report, date));
                        page.Footer().Element(container => ComposeFooter(container, report));
                    });
                });

                return document.GeneratePdf();
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().AlignCenter().Text("RAMA Temple Management System")
                    .FontSize(24)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                column.Item().AlignCenter().PaddingTop(5).Text("End of Day Report")
                    .FontSize(18)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken2);

                column.Item().AlignCenter().PaddingTop(3)
                    .Text($"Generated: {DateTime.Now:MMMM dd, yyyy - hh:mm tt}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(10).PaddingBottom(10)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten2);
            });
        }


        private void ComposeContent(IContainer container, EndOfDayReportDto report, DateTime date)
        {
            container.Column(column =>
            {
                column.Spacing(15);

                // Report Date
                column.Item().Text($"Report Date: {date:MMMM dd, yyyy}")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                // Summary Section
                column.Item().Element(c => ComposeSummarySection(c, report));

                // Donation Type Breakdown
                column.Item().Element(c => ComposeDonationTypeBreakdown(c, report));

                // Payment Method Breakdown
                column.Item().Element(c => ComposePaymentMethodBreakdown(c, report));

                // Donations List
                column.Item().Element(c => ComposeDonationsList(c, report));
            });
        }

        private void ComposeSummarySection(IContainer container, EndOfDayReportDto report)
        {
            container.Column(column =>
            {
                column.Item().Text("Summary")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(120);
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                        columns.RelativeColumn();
                    });

                    // Row 1
                    table.Cell().Text("Total Amount:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    table.Cell().Text($"${report.TotalAmount:N2}").Bold().FontSize(11);
                    table.Cell().Text("Total Donations:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    table.Cell().Text($"{report.TotalCount}").Bold().FontSize(11);

                    // Row 2
                    table.Cell().PaddingTop(8).Text("Unique Donors:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    table.Cell().PaddingTop(8).Text($"{report.UniqueDonors}").Bold().FontSize(11);
                    table.Cell().PaddingTop(8).Text("Average Donation:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    table.Cell().PaddingTop(8).Text($"${report.AverageDonation:N2}").Bold().FontSize(11);
                });
            });
        }

        private void ComposeDonationTypeBreakdown(IContainer container, EndOfDayReportDto report)
        {
            container.Column(column =>
            {
                column.Item().Text("Donation Type Breakdown")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Type").Bold().FontSize(10);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Amount").Bold().FontSize(10);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Count").Bold().FontSize(10);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Percentage").Bold().FontSize(10);
                    });

                    // Rows
                    foreach (var breakdown in report.ByDonationType)
                    {
                        var percentage = (breakdown.Amount / report.TotalAmount) * 100;

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(breakdown.Type).FontSize(9);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"${breakdown.Amount:N2}").FontSize(9);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"{breakdown.Count}").FontSize(9);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"{percentage:F1}%").FontSize(9);
                    }
                });
            });
        }

        private void ComposePaymentMethodBreakdown(IContainer container, EndOfDayReportDto report)
        {
            container.Column(column =>
            {
                column.Item().Text("Payment Method Breakdown")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Payment Method").Bold().FontSize(10);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Amount").Bold().FontSize(10);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Count").Bold().FontSize(10);
                    });

                    // Rows
                    foreach (var breakdown in report.ByPaymentMethod)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(breakdown.Type).FontSize(9);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"${breakdown.Amount:N2}").FontSize(9);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"{breakdown.Count}").FontSize(9);
                    }
                });
            });
        }

        private void ComposeDonationsList(IContainer container, EndOfDayReportDto report)
        {
            container.Column(column =>
            {
                column.Item().Text($"All Donations ({report.Donations.Count})")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Donor Name").Bold().FontSize(9);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Amount").Bold().FontSize(9);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Type").Bold().FontSize(9);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Payment").Bold().FontSize(9);

                        header.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(5)
                            .Text("Time").Bold().FontSize(9);
                    });

                    // Rows
                    foreach (var donation in report.Donations)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(donation.DonorName).FontSize(8);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text($"${donation.Amount:N2}").FontSize(8);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(donation.DonationType).FontSize(8);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(donation.PaymentMode).FontSize(8);

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(donation.Timestamp.ToString("hh:mm tt")).FontSize(8);
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container, EndOfDayReportDto report)
        {
            container.AlignBottom().Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text($"Collector: {report.CollectorName ?? "N/A"}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignRight()
                        .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1))
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            });
        }

    }
}
