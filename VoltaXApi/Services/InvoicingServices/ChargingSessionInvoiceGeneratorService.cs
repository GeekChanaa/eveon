using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoltaxApi.Dtos;

namespace VoltaXApi.Services;

public class ChargingSessionInvoiceGeneratorService
{
    // Brand palette — mirrors the `.vx` dashboard theme tokens (theme.sass).
    private const string Yellow = "#E5B223";
    private const string YellowSoft = "#FBF4E0";
    private const string YellowInk = "#8A6410";
    private const string Text = "#0B0B0B";
    private const string TextMuted = "#7A7C80";
    private const string Border = "#EDEDED";
    private const string SurfaceAlt = "#FAFAFA";

    private const string FontFamily = "Inter";
    private const string Currency = "DH";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");
    private static readonly Lazy<bool> FontsRegistered = new(RegisterFonts);

    private readonly IBusinessClock _clock;

    public ChargingSessionInvoiceGeneratorService(IBusinessClock clock)
    {
        _clock = clock;
    }

    private static bool RegisterFonts()
    {
        foreach (var weight in new[] { "Regular", "Medium", "SemiBold", "Bold" })
        {
            using var stream = File.OpenRead($"Assets/Fonts/Inter-{weight}.ttf");
            FontManager.RegisterFontWithCustomName(FontFamily, stream);
        }
        return true;
    }

    public byte[] GenerateInvoice(ChargingSessionInvoice invoice)
    {
        _ = FontsRegistered.Value;
        var logoPath = "Assets/Images/voltax-logo.svg";

        var totalMinutes = invoice.ChargedMinutes + invoice.IdleMinutes;
        var vatAmount = invoice.TotalPriceWithVAT - invoice.TotalPriceWithoutVAT;
        var vatPercent = invoice.TotalPriceWithoutVAT > 0
            ? Math.Round((invoice.TotalPriceWithVAT / invoice.TotalPriceWithoutVAT - 1) * 100)
            : 0;
        var averagePower = invoice.ChargedMinutes > 0
            ? invoice.KwhsCharged / (invoice.ChargedMinutes / 60)
            : 0;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(36);
                page.MarginHorizontal(44);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(9.5f).FontColor(Text).LineHeight(1.35f));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Height(44).AlignLeft().Svg(SvgImage.FromFile(logoPath)).FitHeight();
                            col.Item().PaddingTop(2).Text("Electric Vehicle Charging").FontSize(9).FontColor(TextMuted);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text("INVOICE").FontSize(22).Bold().LetterSpacing(0.08f);
                            col.Item().AlignRight().Text(text =>
                            {
                                text.Span("No. ").FontColor(TextMuted);
                                text.Span($"CS-{invoice.ChargingSessionID:000000}").SemiBold();
                            });
                            col.Item().AlignRight().Text(text =>
                            {
                                text.Span("Issued ").FontColor(TextMuted);
                                text.Span(FormatDate(_clock.UtcNow, "dd MMM yyyy")).SemiBold();
                            });
                        });
                    });

                    header.Item().PaddingTop(16).Height(3).Background(Yellow);
                });

                page.Content().PaddingTop(22).Column(content =>
                {
                    content.Spacing(20);

                    // Billed to / session details
                    content.Item().Row(row =>
                    {
                        row.Spacing(14);

                        InfoCard(row.RelativeItem(), "BILLED TO", col =>
                        {
                            col.Item().Text(Fallback(invoice.UserName)).FontSize(12).SemiBold();
                            col.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("Card ").FontColor(TextMuted);
                                text.Span(Fallback(invoice.CardNumber)).Medium();
                            });
                        });

                        InfoCard(row.RelativeItem(), "CHARGING LOCATION", col =>
                        {
                            col.Item().Text(Fallback(invoice.ChargePointName)).FontSize(12).SemiBold();
                            col.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("Connector ").FontColor(TextMuted);
                                text.Span(invoice.ConnectorID?.ToString() ?? "—").Medium();
                            });
                        });
                    });

                    // Session timeline
                    content.Item().Border(1).BorderColor(Border).Row(row =>
                    {
                        Stat(row.RelativeItem(), "SESSION DATE", FormatDate(invoice.SessionDate, "dd MMM yyyy"));
                        Divider(row);
                        Stat(row.RelativeItem(), "START TIME", FormatDate(invoice.StartDate, "HH:mm"));
                        Divider(row);
                        Stat(row.RelativeItem(), "END TIME", invoice.EndDate.HasValue
                            ? FormatDate(invoice.EndDate.Value, _clock.ToLocal(invoice.EndDate.Value).Date == _clock.ToLocal(invoice.StartDate).Date ? "HH:mm" : "dd MMM, HH:mm")
                            : "In progress");
                        Divider(row);
                        Stat(row.RelativeItem(), "DURATION", FormatDuration(totalMinutes));
                        Divider(row);
                        Stat(row.RelativeItem(), "ENERGY", $"{invoice.KwhsCharged.ToString("0.00", Culture)} kWh");
                    });

                    // Pricing breakdown
                    content.Item().Column(col =>
                    {
                        col.Item().PaddingBottom(8).Text("Pricing breakdown").FontSize(12).SemiBold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header.Cell(), "DESCRIPTION");
                                HeaderCell(header.Cell(), "QUANTITY", alignRight: true);
                                HeaderCell(header.Cell(), "RATE", alignRight: true);
                                HeaderCell(header.Cell(), "AMOUNT", alignRight: true);
                            });

                            LineRow(table, "Charging time", "Active charging",
                                $"{invoice.ChargedMinutes.ToString("0.00", Culture)} min",
                                $"{Money(invoice.PricePerMinute)} / min",
                                Money(invoice.ChargingPriceWithVAT));

                            if (invoice.IdleMinutes > 0)
                            {
                                LineRow(table, "Idle time", "Connected after charging ended",
                                    $"{invoice.IdleMinutes.ToString("0.00", Culture)} min",
                                    $"{Money(invoice.PricePerIdleMinute)} / min",
                                    Money(invoice.IdlePriceWithVAT));
                            }

                            LineRow(table, "Energy delivered", "Included in time-based pricing",
                                $"{invoice.KwhsCharged.ToString("0.00", Culture)} kWh",
                                "Included",
                                "—");
                        });

                        col.Item().PaddingTop(4).Text("Amounts include VAT.").FontSize(8).FontColor(TextMuted);
                    });

                    // Payment details + totals
                    content.Item().Row(row =>
                    {
                        row.Spacing(14);

                        InfoCard(row.RelativeItem(), "PAYMENT", col =>
                        {
                            KeyValue(col, "Method", "Recharge card");
                            KeyValue(col, "Balance before", Money(invoice.CardBalance + invoice.TotalPriceWithVAT));
                            KeyValue(col, "Balance after", Money(invoice.CardBalance), highlight: true);
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().PaddingHorizontal(14).PaddingVertical(4).Column(inner =>
                            {
                                TotalLine(inner, "Subtotal (excl. VAT)", Money(invoice.TotalPriceWithoutVAT));
                                TotalLine(inner, $"VAT ({vatPercent.ToString("0", Culture)}%)", Money(vatAmount));
                            });

                            col.Item().PaddingTop(6).BorderLeft(3).BorderColor(Yellow).Background(YellowSoft)
                                .PaddingHorizontal(14).PaddingVertical(12).Row(total =>
                                {
                                    total.RelativeItem().AlignMiddle().Text("Total paid").FontSize(11).SemiBold();
                                    total.AutoItem().AlignMiddle().Text(Money(invoice.TotalPriceWithVAT)).FontSize(17).Bold();
                                });
                        });
                    });

                    // Session statistics
                    content.Item().Background(SurfaceAlt).Border(1).BorderColor(Border).Padding(14).Row(row =>
                    {
                        Metric(row.RelativeItem(), "Charging", FormatDuration(invoice.ChargedMinutes));
                        Metric(row.RelativeItem(), "Idle", FormatDuration(invoice.IdleMinutes));
                        Metric(row.RelativeItem(), "Average power", $"{averagePower.ToString("0.0", Culture)} kW");
                        Metric(row.RelativeItem(), "Charging ratio", totalMinutes > 0
                            ? $"{(invoice.ChargedMinutes / totalMinutes * 100).ToString("0", Culture)}%"
                            : "—");
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Border);
                    footer.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Thank you for charging with VoltaX.").SemiBold().FontSize(9);
                            col.Item().Text("For support, visit www.voltax.com or call +1 234 567 890").FontSize(8).FontColor(TextMuted);
                        });

                        row.AutoItem().AlignRight().AlignBottom().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted));
                            text.Span($"Generated {FormatDate(_clock.UtcNow, "dd MMM yyyy, HH:mm")}  ·  Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void InfoCard(IContainer container, string title, Action<ColumnDescriptor> body)
    {
        // Chained border calls merge into one element with a single color,
        // so the yellow accent rail is drawn as its own column.
        container.Border(1).BorderColor(Border).Row(row =>
        {
            row.ConstantItem(3).Background(Yellow);
            row.RelativeItem().PaddingVertical(12).PaddingHorizontal(14).Column(col =>
            {
                col.Item().PaddingBottom(6).Text(title).FontSize(7.5f).SemiBold().FontColor(YellowInk).LetterSpacing(0.08f);
                body(col);
            });
        });
    }

    private static void Stat(IContainer container, string label, string value)
    {
        container.PaddingVertical(10).PaddingHorizontal(12).Column(col =>
        {
            col.Item().Text(label).FontSize(7).SemiBold().FontColor(TextMuted).LetterSpacing(0.08f);
            col.Item().PaddingTop(3).Text(value).FontSize(10.5f).SemiBold();
        });
    }

    private static void Divider(RowDescriptor row) =>
        row.ConstantItem(1).PaddingVertical(8).Background(Border);

    private static void Metric(IContainer container, string label, string value)
    {
        container.Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor(TextMuted);
            col.Item().Text(value).FontSize(11).SemiBold();
        });
    }

    private static void HeaderCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container.Background(Text).PaddingVertical(8).PaddingHorizontal(10);
        (alignRight ? cell.AlignRight() : cell)
            .Text(text).FontSize(7.5f).SemiBold().FontColor(Colors.White).LetterSpacing(0.08f);
    }

    private static void LineRow(TableDescriptor table, string title, string subtitle, string quantity, string rate, string amount)
    {
        IContainer Cell() => table.Cell().BorderBottom(1).BorderColor(Border).PaddingVertical(10).PaddingHorizontal(10);

        Cell().Column(col =>
        {
            col.Item().Text(title).Medium();
            col.Item().Text(subtitle).FontSize(8).FontColor(TextMuted);
        });
        Cell().AlignRight().AlignMiddle().Text(quantity);
        Cell().AlignRight().AlignMiddle().Text(rate).FontColor(TextMuted);
        Cell().AlignRight().AlignMiddle().Text(amount).SemiBold();
    }

    private static void KeyValue(ColumnDescriptor col, string key, string value, bool highlight = false)
    {
        col.Item().PaddingVertical(2).Row(row =>
        {
            row.RelativeItem().Text(key).FontColor(TextMuted);
            var text = row.AutoItem().Text(value).Medium();
            if (highlight) text.SemiBold().FontColor(YellowInk);
        });
    }

    private static void TotalLine(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(4).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(TextMuted);
            row.AutoItem().Text(value).Medium();
        });
    }

    private static string Money(double amount) => $"{amount.ToString("N2", Culture)} {Currency}";

    // Stored instants are UTC; invoices show business-time-zone wall-clock time.
    private string FormatDate(DateTime date, string format) =>
        date == default ? "—" : _clock.ToLocal(date).ToString(format, Culture);

    private static string FormatDuration(double minutes)
    {
        if (minutes <= 0) return "0 min";
        var span = TimeSpan.FromMinutes(minutes);
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours} h {span.Minutes:00} min";
        return span.Seconds > 0 && span.Minutes < 10 ? $"{span.Minutes} min {span.Seconds:00} s" : $"{span.Minutes} min";
    }

    private static string Fallback(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
