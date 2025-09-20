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
        FontManager.RegisterFont(File.OpenRead("Assets/Fonts/OpenSans-Regular.ttf"));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                // Header Section with Logo and Branding
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeColumn(7).Column(col =>
                        {
                            col.Item().Text("VoltaX").Bold().FontSize(28).FontColor("#6366f1");
                            col.Item().Text("Electric Vehicle Charging").FontSize(12).FontColor("#64748b");
                        });

                        row.RelativeColumn(5).Column(col =>
                        {
                            col.Item().AlignRight().Text("CHARGING SESSION INVOICE").Bold().FontSize(16).FontColor("#1e293b");
                            col.Item().AlignRight().Text($"Session #{invoice.ChargingSessionID}").FontSize(12).FontColor("#64748b");
                        });
                    });

                    header.Item().PaddingTop(20).LineHorizontal(2).LineColor("#6366f1");
                });

                // Content Section
                page.Content().PaddingVertical(30).Column(content =>
                {
                    content.Spacing(25);

                    // Session Information Section
                    content.Item().Column(col =>
                    {
                        col.Item().Text("SESSION INFORMATION").Bold().FontSize(14).FontColor("#1e293b");
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeColumn(6).Column(leftCol =>
                            {
                                leftCol.Item().Text("CHARGED BY:").Bold().FontSize(10).FontColor("#64748b");
                                leftCol.Item().Text($"{invoice.UserName}").FontSize(12);
                                leftCol.Item().PaddingTop(5).Text("CHARGE POINT:").Bold().FontSize(10).FontColor("#64748b");
                                leftCol.Item().Text($"{invoice.ChargePointName}").FontSize(12);
                                leftCol.Item().PaddingTop(5).Text("CONNECTOR:").Bold().FontSize(10).FontColor("#64748b");
                                leftCol.Item().Text($"{invoice.ConnectorID}").FontSize(12);
                            });

                            row.RelativeColumn(6).Column(rightCol =>
                            {
                                rightCol.Item().Text("SESSION DATE:").Bold().FontSize(10).FontColor("#64748b");
                                rightCol.Item().Text($"{invoice.StartDate:dd.MM.yyyy HH:mm}").FontSize(12);
                                rightCol.Item().PaddingTop(5).Text("END DATE:").Bold().FontSize(10).FontColor("#64748b");
                                rightCol.Item().Text($"{invoice.EndDate:dd.MM.yyyy HH:mm}").FontSize(12);
                                rightCol.Item().PaddingTop(5).Text("CARD USED:").Bold().FontSize(10).FontColor("#64748b");
                                rightCol.Item().Text($"{invoice.CardNumber}").FontSize(12);
                            });
                        });
                    });

                    // Separator
                    content.Item().LineHorizontal(1).LineColor("#e2e8f0");

                    // Pricing Details Section
                    content.Item().Column(col =>
                    {
                        col.Item().Text("PRICING BREAKDOWN").Bold().FontSize(14).FontColor("#1e293b");

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Background("#f8fafc").Padding(12).Text("DESCRIPTION").Bold().FontSize(10);
                                header.Cell().Background("#f8fafc").Padding(12).Text("QUANTITY").Bold().FontSize(10);
                                header.Cell().Background("#f8fafc").Padding(12).Text("RATE").Bold().FontSize(10);
                                header.Cell().Background("#f8fafc").Padding(12).Text("AMOUNT").Bold().FontSize(10);
                            });

                            // Charging Time Row
                            table.Cell().Padding(12).Text($"Charging Time").FontSize(10);
                            table.Cell().Padding(12).Text($"{invoice.ChargedMinutes:F2} minutes").FontSize(10);
                            table.Cell().Padding(12).Text($"{invoice.PricePerMinute:F2} DH/min").FontSize(10);
                            table.Cell().Padding(12).Text($"{(invoice.ChargedMinutes * invoice.PricePerMinute):F2} DH").FontSize(10);

                            // Idle Time Row (if applicable)
                            if (invoice.IdleMinutes > 0)
                            {
                                table.Cell().Padding(12).Text($"Idle Time").FontSize(10);
                                table.Cell().Padding(12).Text($"{invoice.IdleMinutes:F2} minutes").FontSize(10);
                                table.Cell().Padding(12).Text($"{invoice.PricePerIdleMinute:F2} DH/min").FontSize(10);
                                table.Cell().Padding(12).Text($"{(invoice.IdleMinutes * invoice.PricePerIdleMinute):F2} DH").FontSize(10);
                            }

                            // Energy Consumption Row
                            table.Cell().Padding(12).Text($"Energy Consumed").FontSize(10);
                            table.Cell().Padding(12).Text($"{invoice.KwhsCharged:F2} kWh").FontSize(10);
                            table.Cell().Padding(12).Text($"Included").FontSize(10);
                            table.Cell().Padding(12).Text($"-").FontSize(10);
                        });
                    });

                    // Summary Section
                    content.Item().AlignRight().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(8);

                        // Subtotal
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn(3).Text("SUBTOTAL (Excl. VAT):").Bold().FontSize(11);
                            row.RelativeColumn(2).AlignRight().Text($"{invoice.TotalPriceWithoutVAT:F2} DH").FontSize(11);
                        });

                        // VAT
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn(3).Text("VAT (20%):").Bold().FontSize(11);
                            row.RelativeColumn(2).AlignRight().Text($"{(invoice.TotalPriceWithVAT - invoice.TotalPriceWithoutVAT):F2} DH").FontSize(11);
                        });

                        // Line separator
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#e2e8f0");

                        // Total
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeColumn(3).Text("TOTAL AMOUNT:").Bold().FontSize(14).FontColor("#1e293b");
                            row.RelativeColumn(2).AlignRight().Text($"{invoice.TotalPriceWithVAT:F2} DH").Bold().FontSize(14).FontColor("#059669");
                        });
                    });

                    // Payment Information
                    content.Item().PaddingTop(30).Column(col =>
                    {
                        col.Item().Text("PAYMENT INFORMATION").Bold().FontSize(12).FontColor("#1e293b");
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeColumn(6).Column(leftCol =>
                            {
                                leftCol.Item().Text("PAYMENT METHOD:").Bold().FontSize(10).FontColor("#64748b");
                                leftCol.Item().Text($"Recharge Card").FontSize(11);
                                leftCol.Item().PaddingTop(5).Text("CARD NUMBER:").Bold().FontSize(10).FontColor("#64748b");
                                leftCol.Item().Text($"{invoice.CardNumber}").FontSize(11);
                            });

                            row.RelativeColumn(6).Column(rightCol =>
                            {
                                rightCol.Item().Text("CARD BALANCE BEFORE:").Bold().FontSize(10).FontColor("#64748b");
                                rightCol.Item().Text($"{(invoice.CardBalance + invoice.TotalPriceWithVAT):F2} DH").FontSize(11);
                                rightCol.Item().PaddingTop(5).Text("CARD BALANCE AFTER:").Bold().FontSize(10).FontColor("#64748b");
                                rightCol.Item().Text($"{invoice.CardBalance:F2} DH").FontSize(11).FontColor("#059669");
                            });
                        });
                    });

                    // Session Statistics
                    content.Item().PaddingTop(20).Background("#f8fafc").Padding(15).Column(col =>
                    {
                        col.Item().Text("SESSION STATISTICS").Bold().FontSize(12).FontColor("#1e293b");
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Total Duration").Bold().FontSize(9).FontColor("#64748b");
                                c.Item().Text($"{(invoice.ChargedMinutes + invoice.IdleMinutes):F0} min").FontSize(11);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Energy Delivered").Bold().FontSize(9).FontColor("#64748b");
                                c.Item().Text($"{invoice.KwhsCharged:F2} kWh").FontSize(11);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Average Power").Bold().FontSize(9).FontColor("#64748b");
                                c.Item().Text($"{(invoice.KwhsCharged / (invoice.ChargedMinutes / 60)):F1} kW").FontSize(11);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Efficiency").Bold().FontSize(9).FontColor("#64748b");
                                c.Item().Text($"{((invoice.ChargedMinutes / (invoice.ChargedMinutes + invoice.IdleMinutes)) * 100):F0}%").FontSize(11);
                            });
                        });
                    });
                });

                // Footer
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor("#e2e8f0");
                    footer.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeColumn().Text("Thank you for choosing VoltaX!").FontSize(10).FontColor("#64748b");
                        row.RelativeColumn().AlignRight().Text($"Generated on {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).FontColor("#9ca3af");
                    });
                    footer.Item().PaddingTop(5).AlignCenter().Text("For support, visit www.voltax.com or call +1 234 567 890").FontSize(8).FontColor("#9ca3af");
                });
            });
        }).GeneratePdf();
    }
}