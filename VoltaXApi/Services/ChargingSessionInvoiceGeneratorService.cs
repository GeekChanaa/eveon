using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using VoltaxApi.Dtos;

namespace VoltaXApi.Services;
public class ChargingSessionInvoiceGeneratorService
{
    public byte[] GenerateInvoice(ChargingSessionInvoice invoice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text($"Invoice for {invoice.UserName}").Bold().FontSize(20).AlignCenter();
                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"Session Date: {invoice.SessionDate:yyyy-MM-dd}");
                    column.Item().Text($"Charge Point: {invoice.ChargePointName}");
                    column.Item().Text($"Total kWh Charged: {invoice.TotalKwhCharged:F2} kWh");
                    column.Item().Text($"Total Price: ${invoice.TotalPrice:F2}");

                    column.Item().LineHorizontal(1);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Text("Start Time").Bold();
                            header.Cell().Text("Stop Time").Bold();
                            header.Cell().Text("Meter Start").Bold();
                            header.Cell().Text("Meter Stop").Bold();
                            header.Cell().Text("Amount").Bold();
                        });

                        // Table Rows
                        foreach (var transaction in invoice.Transactions)
                        {
                            table.Cell().Text(transaction.StartTime.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Text(transaction.StopTime?.ToString("yyyy-MM-dd HH:mm") ?? "Ongoing");
                            table.Cell().Text(transaction.MeterStart.ToString("F2"));
                            table.Cell().Text(transaction.MeterStop?.ToString("F2") ?? "N/A");
                            table.Cell().Text($"${transaction.Amount:F2}");
                        }
                    });
                });

                page.Footer().Text(text =>
                {
                    text.Span("Thank you for using our service!");
                    text.AlignCenter();
                });
            });
        }).GeneratePdf();
    }
}
