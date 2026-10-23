using System.Globalization;
using System.Text;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services.CustomerPortal
{
    /// <summary>
    /// Charging sessions of one customer over a period: a printable PDF statement and a CSV for spreadsheets.
    /// Amounts include VAT; times are business-time-zone wall-clock times.
    /// </summary>
    public sealed class SessionStatementExporter
    {
        private const string Navy = "#0A1A3A";
        private const string NavyMid = "#1F3461";
        private const string Muted = "#4A5878";
        private const string Border = "#DFE5F0";
        private const string Soft = "#EEF2F9";
        private const string FontFamily = "Inter";
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");
        private static readonly Lazy<bool> FontsRegistered = new(() =>
        {
            foreach (var weight in new[] { "Regular", "Medium", "SemiBold", "Bold" })
            {
                using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", $"Inter-{weight}.ttf"));
                FontManager.RegisterFontWithCustomName(FontFamily, stream);
            }
            return true;
        });

        private readonly IBusinessClock _clock;

        public SessionStatementExporter(IBusinessClock clock)
        {
            _clock = clock;
        }

        public byte[] Pdf(string customerName, DateTime? from, DateTime? to, IReadOnlyList<MySessionDto> sessions, decimal vatRate)
        {
            _ = FontsRegistered.Value;
            var total = sessions.Sum(s => s.Total);
            var (excl, vat) = CostCalculator.SplitVat(total, vatRate);
            var logo = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "eveon-logo.svg");

            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(8.5f).FontColor(Navy));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Height(30).AlignLeft().Svg(SvgImage.FromFile(logo)).FitHeight();
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text("CHARGING STATEMENT").FontSize(16).Bold().LetterSpacing(0.06f);
                            col.Item().AlignRight().Text(customerName).FontSize(10).SemiBold();
                            col.Item().AlignRight().Text(Period(from, to)).FontColor(Muted);
                        });
                    });
                    header.Item().PaddingTop(10).Height(2).Background(Navy);
                });

                page.Content().PaddingTop(14).Column(content =>
                {
                    content.Spacing(12);
                    content.Item().Row(row =>
                    {
                        Summary(row.RelativeItem(), "SESSIONS", sessions.Count.ToString(Culture));
                        Summary(row.RelativeItem(), "ENERGY", $"{sessions.Sum(s => s.ChargedKwhs).ToString("N2", Culture)} kWh");
                        Summary(row.RelativeItem(), "CHARGING TIME", $"{sessions.Sum(s => s.ChargedMinutes).ToString("N0", Culture)} min");
                        Summary(row.RelativeItem(), "TOTAL (VAT INCL.)", Money(total));
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.6f); c.RelativeColumn(2.6f); c.RelativeColumn(1.2f); c.RelativeColumn(1f);
                            c.RelativeColumn(1f); c.RelativeColumn(1.1f); c.RelativeColumn(1.1f); c.RelativeColumn(1f); c.RelativeColumn(1f); c.RelativeColumn(1.2f);
                        });
                        table.Header(h =>
                        {
                            foreach (var (title, right) in new[] { ("DATE", false), ("STATION", false), ("CONNECTOR", false), ("MINUTES", true), ("KWH", true),
                                         ("ENERGY", true), ("TIME", true), ("FEES", true), ("IDLE", true), ("TOTAL", true) })
                            {
                                var cell = h.Cell().Background(Navy).PaddingVertical(6).PaddingHorizontal(6);
                                (right ? cell.AlignRight() : cell).Text(title).FontSize(7).SemiBold().FontColor(Colors.White);
                            }
                        });
                        var odd = false;
                        foreach (var s in sessions)
                        {
                            string background = (odd = !odd) ? "#FFFFFF" : Soft;
                            IContainer Cell() => table.Cell().Background(background).BorderBottom(1).BorderColor(Border).PaddingVertical(5).PaddingHorizontal(6);
                            Cell().Text(_clock.ToLocal(s.StartDate).ToString("dd MMM yyyy HH:mm", Culture));
                            Cell().Text(string.Join(" · ", new[] { s.StationName, s.ChargePointName }.Where(v => !string.IsNullOrEmpty(v))));
                            Cell().Text(s.Connector ?? "—");
                            Cell().AlignRight().Text(s.ChargedMinutes.ToString("N0", Culture));
                            Cell().AlignRight().Text(s.ChargedKwhs.ToString("N2", Culture));
                            Cell().AlignRight().Text(s.EnergyAmount.ToString("N2", Culture));
                            Cell().AlignRight().Text(s.TimeAmount.ToString("N2", Culture));
                            Cell().AlignRight().Text(s.FlatFeeAmount.ToString("N2", Culture));
                            Cell().AlignRight().Text(s.IdleAmount.ToString("N2", Culture));
                            Cell().AlignRight().Text(s.Total.ToString("N2", Culture)).SemiBold();
                        }
                    });

                    content.Item().AlignRight().Width(220).Column(col =>
                    {
                        Line(col, "Total excl. VAT", Money(excl));
                        Line(col, $"VAT ({(vatRate * 100).ToString("0.#", Culture)}%)", Money(vat));
                        col.Item().PaddingTop(4).BorderTop(2).BorderColor(Navy).PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total").SemiBold();
                            r.AutoItem().Text(Money(total)).Bold().FontColor(NavyMid);
                        });
                    });
                    content.Item().Text("Amounts in MAD, VAT included. Session invoices are available per session in your EVEON account.")
                        .FontSize(7.5f).FontColor(Muted);
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span($"Generated {_clock.LocalNow.ToString("dd MMM yyyy HH:mm", Culture)} · page ").FontColor(Muted);
                    t.CurrentPageNumber().FontColor(Muted);
                    t.Span(" / ").FontColor(Muted);
                    t.TotalPages().FontColor(Muted);
                });
            })).GeneratePdf();
        }

        /// <summary>Semicolon separated (spreadsheet default in French / Arabic locales), UTF-8 with BOM, dot decimals.</summary>
        public byte[] Csv(IReadOnlyList<MySessionDto> sessions)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Session;Start;End;Station;Charge point;Connector;Minutes;Idle minutes;kWh;Energy (MAD);Time (MAD);Session fee (MAD);Idle fee (MAD);Total (MAD);Stop reason");
            foreach (var s in sessions)
            {
                csv.AppendLine(string.Join(";",
                    s.Id.ToString(CultureInfo.InvariantCulture),
                    Local(s.StartDate),
                    s.EndDate.HasValue ? Local(s.EndDate.Value) : "",
                    Field(s.StationName), Field(s.ChargePointName), Field(s.Connector),
                    s.ChargedMinutes.ToString("0.##", CultureInfo.InvariantCulture),
                    s.IdleMinutes.ToString("0.##", CultureInfo.InvariantCulture),
                    s.ChargedKwhs.ToString("0.###", CultureInfo.InvariantCulture),
                    s.EnergyAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    s.TimeAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    s.FlatFeeAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    s.IdleAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    s.Total.ToString("0.00", CultureInfo.InvariantCulture),
                    s.Active ? "In progress" : s.StoppedReason.ToString()));
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        private string Local(DateTime utc) => _clock.ToLocal(utc).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        private static string Field(string? value) => Helpers.CsvField.Of(value);

        private string Period(DateTime? from, DateTime? to) => (from, to) switch
        {
            (null, null) => "All sessions",
            ({ } f, null) => $"From {f.ToString("dd MMM yyyy", Culture)}",
            (null, { } t) => $"Until {t.ToString("dd MMM yyyy", Culture)}",
            ({ } f, { } t) => $"{f.ToString("dd MMM yyyy", Culture)} – {t.ToString("dd MMM yyyy", Culture)}"
        };

        private static void Summary(IContainer container, string label, string value) =>
            container.PaddingRight(10).Border(1).BorderColor(Border).Background(Soft).Padding(10).Column(col =>
            {
                col.Item().Text(label).FontSize(7).FontColor(Muted).LetterSpacing(0.06f);
                col.Item().Text(value).FontSize(12).SemiBold();
            });

        private static void Line(ColumnDescriptor col, string label, string value) =>
            col.Item().PaddingVertical(2).Row(r =>
            {
                r.RelativeItem().Text(label).FontColor(Muted);
                r.AutoItem().Text(value);
            });

        private static string Money(decimal amount) => $"{amount.ToString("N2", Culture)} MAD";
    }
}
