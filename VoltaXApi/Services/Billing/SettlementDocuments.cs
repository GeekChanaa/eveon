using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    /// <summary>A partner settlement as a CSV file (one row per line, then the totals) or a printable PDF statement.</summary>
    public sealed class SettlementDocuments
    {
        private const string Ink = "#0A1A3A";
        private const string Muted = "#5B6680";
        private const string Border = "#DFE5F0";
        private const string Soft = "#F3F6FB";
        private const string Accent = "#E5B223";
        private readonly IBusinessClock _clock;

        public SettlementDocuments(IBusinessClock clock)
        {
            _clock = clock;
        }

        public static string FileName(SettlementDto s) => $"eveon-statement-{s.Year}-{s.Month:00}-partner-{s.PartnerId}";

        /// <summary>Semicolon separated, UTF-8 with BOM, dot decimals; the last row holds the totals.</summary>
        public byte[] Csv(SettlementDetailDto detail)
        {
            var s = detail.Settlement;
            var csv = new StringBuilder();
            csv.AppendLine("Line;Date;Session;Refund;Station;Charge point;kWh;Gross incl. VAT (MAD);Net excl. VAT (MAD);VAT (MAD);Commission excl. VAT (MAD);VAT on commission (MAD);Net payout (MAD)");
            foreach (var l in detail.Lines)
                csv.AppendLine(string.Join(";", l.SourceKey, _clock.ToLocal(l.Date).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    l.ChargingSessionId?.ToString(CultureInfo.InvariantCulture) ?? "", l.RefundId?.ToString(CultureInfo.InvariantCulture) ?? "",
                    CsvField.Of(l.Station), CsvField.Of(l.ChargePoint), N(l.Kwh, "0.###"), N(l.Gross), N(l.NetExclVat), N(l.Vat), N(l.CommissionExclVat),
                    N(l.CommissionVat), N(l.NetPayout)));
            csv.AppendLine(string.Join(";", "TOTAL", $"{s.Year}-{s.Month:00}", s.SessionCount.ToString(CultureInfo.InvariantCulture), "", CsvField.Of(s.PartnerName), "",
                N(s.EnergyKwh, "0.###"), N(s.GrossAmount), N(s.NetExclVat), N(s.RevenueVat), N(s.CommissionExclVat), N(s.CommissionVat), N(s.NetPayout)));
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        public byte[] Pdf(SettlementDetailDto detail, CompanyInformations company, string language)
        {
            BillingFonts.Ensure();
            var s = detail.Settlement;
            var lang = BillingTexts.Normalize(language);
            var rtl = BillingTexts.IsRightToLeft(lang);
            string L(string key) => BillingTexts.Get(lang, key);
            string Money(decimal amount) => BillingTexts.Money(amount, lang);
            var culture = BillingTexts.NumberCulture(lang);
            var logo = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "eveon-logo.svg");
            IContainer End(IContainer c) => rtl ? c.AlignLeft() : c.AlignRight();

            return Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                if (rtl)
                    page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily(BillingFonts.For(lang)).FontSize(8.5f).FontColor(Ink));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Height(28).Svg(SvgImage.FromFile(logo)).FitHeight();
                        row.RelativeItem().Column(col =>
                        {
                            End(col.Item()).Text(L("Statement")).FontSize(16).Bold();
                            End(col.Item()).Text(s.PartnerName).FontSize(10).SemiBold();
                            End(col.Item()).Text($"{L("Period")} : {PartnerSettlementService.PeriodLabel(s.Year, s.Month, lang)}").FontColor(Muted);
                        });
                    });
                    header.Item().PaddingTop(8).Height(3).Background(Accent);
                });

                page.Content().PaddingTop(12).Column(content =>
                {
                    content.Spacing(10);
                    content.Item().Row(row =>
                    {
                        row.Spacing(8);
                        Tile(row.RelativeItem(), L("Sessions"), s.SessionCount.ToString(culture));
                        Tile(row.RelativeItem(), L("Energy"), $"{s.EnergyKwh.ToString("N3", culture)} kWh");
                        Tile(row.RelativeItem(), L("Gross"), Money(s.GrossAmount));
                        Tile(row.RelativeItem(), L("Commission"), Money(s.CommissionExclVat));
                        Tile(row.RelativeItem(), L("NetPayout"), Money(s.NetPayout), highlight: true);
                    });
                    content.Item().Text(t =>
                    {
                        t.Span(BillingTexts.Format(lang, "CommissionTerms", s.CommissionPercent.ToString("0.##", culture), s.CommissionPerKwh.ToString("0.####", culture))).FontColor(Muted);
                        t.Span("   ·   " + L("Status") + " : ").FontColor(Muted);
                        t.Span(s.Status == SettlementStatus.Paid && s.PaidAt.HasValue
                            ? BillingTexts.Format(lang, "PaidOn", _clock.ToLocal(s.PaidAt.Value).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), s.PaymentReference ?? "")
                            : L("Pending")).SemiBold();
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.3f); c.RelativeColumn(2.6f); c.RelativeColumn(0.9f); c.RelativeColumn(1.2f); c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f); c.RelativeColumn(1.2f); c.RelativeColumn(1.1f); c.RelativeColumn(1.2f);
                        });
                        table.Header(h =>
                        {
                            foreach (var (title, end) in new[] { (L("Date"), false), (L("Station"), false), ("kWh", true), (L("Gross"), true), (L("NetExclVat"), true),
                                         (L("Vat"), true), (L("Commission"), true), (L("CommissionVat"), true), (L("NetPayout"), true) })
                            {
                                var cell = h.Cell().Background(Ink).PaddingVertical(5).PaddingHorizontal(5);
                                (end ? End(cell) : cell).Text(title.ToUpperInvariant()).FontSize(6.5f).SemiBold().FontColor(Colors.White);
                            }
                        });
                        var odd = false;
                        foreach (var l in detail.Lines)
                        {
                            var background = (odd = !odd) ? "#FFFFFF" : Soft;
                            IContainer Cell() => table.Cell().Background(background).BorderBottom(1).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(5);
                            Cell().Text(_clock.ToLocal(l.Date).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
                            Cell().Text(l.RefundId.HasValue
                                ? BillingTexts.Format(lang, "RefundAdjustment", l.ChargingSessionId ?? 0)
                                : $"#{l.ChargingSessionId} · {string.Join(" · ", new[] { l.Station, l.ChargePoint }.Where(v => !string.IsNullOrEmpty(v)))}");
                            End(Cell()).Text(l.Kwh.ToString("N3", culture));
                            End(Cell()).Text(l.Gross.ToString("N2", culture));
                            End(Cell()).Text(l.NetExclVat.ToString("N2", culture));
                            End(Cell()).Text(l.Vat.ToString("N2", culture));
                            End(Cell()).Text(l.CommissionExclVat.ToString("N2", culture));
                            End(Cell()).Text(l.CommissionVat.ToString("N2", culture));
                            End(Cell()).Text(l.NetPayout.ToString("N2", culture)).SemiBold();
                        }
                        IContainer Total() => table.Cell().BorderTop(2).BorderColor(Ink).PaddingVertical(5).PaddingHorizontal(5);
                        Total().Text("TOTAL").Bold();
                        Total().Text($"{s.SessionCount.ToString(culture)} {L("Sessions").ToLower(culture)}");
                        End(Total()).Text(s.EnergyKwh.ToString("N3", culture)).Bold();
                        End(Total()).Text(s.GrossAmount.ToString("N2", culture)).Bold();
                        End(Total()).Text(s.NetExclVat.ToString("N2", culture)).Bold();
                        End(Total()).Text(s.RevenueVat.ToString("N2", culture)).Bold();
                        End(Total()).Text(s.CommissionExclVat.ToString("N2", culture)).Bold();
                        End(Total()).Text(s.CommissionVat.ToString("N2", culture)).Bold();
                        End(Total()).Text(s.NetPayout.ToString("N2", culture)).Bold();
                    });
                    content.Item().Text(L("PricesNote")).FontSize(7).FontColor(Muted);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(7).FontColor(Muted));
                        t.Span(string.IsNullOrWhiteSpace(company.CompanyName) ? "EVEON" : company.CompanyName);
                        var ids = string.Join(" · ", new[] { ("ICE", company.ICE), ("IF", company.IF), ("RC", company.RC) }
                            .Where(p => !string.IsNullOrWhiteSpace(p.Item2)).Select(p => $"{p.Item1} {p.Item2}"));
                        if (ids.Length > 0)
                            t.Span("  ·  " + ids);
                    });
                    row.AutoItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(7).FontColor(Muted));
                        t.Span($"{_clock.LocalNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}  ·  {L("Page")} ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            })).GeneratePdf();

            void Tile(IContainer c, string label, string value, bool highlight = false) =>
                c.Border(1).BorderColor(highlight ? Accent : Border).Background(highlight ? "#FBF4E0" : Soft).Padding(8).Column(col =>
                {
                    col.Item().Text(label).FontSize(7).FontColor(Muted);
                    col.Item().Text(value).FontSize(11).SemiBold();
                });
        }

        private static string N(decimal value, string format = "0.00") => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
