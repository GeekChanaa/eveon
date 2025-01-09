using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using VoltaxApi.Dtos;

namespace VoltaXApi.Services;
public class ChargingSessionInvoiceGeneratorService
{
    public byte[] GenerateInvoice(ChargingSessionInvoice invoice)
    {
        FontManager.RegisterFont(File.OpenRead("Fonts/OpenSans-Regular.ttf"));
        FontManager.RegisterFont(File.OpenRead("Fonts/OpenSans-Bold.ttf"));
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                // Header Section
                page.Header().Column(header =>
                {
                    header.Item().AlignCenter().Text("INVOICE").Bold().FontSize(20).FontColor("#333");
                });

                // Content Section
                page.Content().PaddingVertical(20).Column(content =>
                {
                    content.Spacing(15);

                    // Issued To Section
                    content.Item().Row(row =>
                    {
                        row.RelativeColumn(6).Column(col =>
                        {
                            col.Item().Text("ISSUED TO:").Bold().FontSize(10);
                            col.Item().Text($"{invoice.UserName}");
                            //col.Item().Text($"{invoice.UserAddress}");
                        });

                        row.RelativeColumn(6).Column(col =>
                        {
                            col.Item().Text($"INVOICE NO:").Bold().FontSize(10);
                            //col.Item().Text($"{invoice.InvoiceNumber}");
                            col.Item().Text($"DATE: {invoice.SessionDate:dd.MM.yyyy}");
                            //col.Item().Text($"DUE DATE: {invoice.DueDate:dd.MM.yyyy}");
                        });
                    });

                    // Pay To Section
                    content.Item().Row(row =>
                    {
                        row.RelativeColumn(6).Column(col =>
                        {
                            col.Item().Text("PAY TO:").Bold().FontSize(10);
                            col.Item().Text($"Your Company Name");
                            col.Item().Text($"Bank: Your Bank Name");
                            col.Item().Text($"Account No: 1234-5678-9000");
                        });
                    });

                    // Line Separator
                    content.Item().LineHorizontal(1);

                    // Table Section
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Text("DESCRIPTION").Bold();
                            header.Cell().Text("UNIT PRICE").Bold();
                            header.Cell().Text("QTY").Bold();
                            header.Cell().Text("TOTAL").Bold();
                        });

                        // Table Rows
                        foreach (var transaction in invoice.Transactions)
                        {
                            table.Cell().Text("description");
                            table.Cell().Text($"12");
                            table.Cell().Text((transaction.MeterStop - transaction.MeterStart).ToString());
                            table.Cell().Text($"${transaction.Amount:F2}");
                        }
                    });

                    // Subtotal, Tax, Total
                    content.Item().AlignRight().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("SUBTOTAL:").Bold();
                            //row.RelativeColumn().Text($"${invoice.Subtotal:F2}");
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("TAX (10%):").Bold();
                            //row.RelativeColumn().Text($"${invoice.Tax:F2}");
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("TOTAL:").Bold().FontSize(12);
                            //row.RelativeColumn().Text($"${invoice.Total:F2}").Bold().FontSize(12);
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for your business!").FontSize(10).FontColor("#666");
            });
        }).GeneratePdf();
    }
}
