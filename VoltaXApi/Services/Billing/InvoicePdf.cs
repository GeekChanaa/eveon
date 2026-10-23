using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    /// <summary>One line of an invoice; <see cref="Amount"/> includes VAT, as every EVEON price.</summary>
    public sealed record InvoiceLine(string Description, string? Detail, string Quantity, string? UnitPrice, decimal Amount);

    /// <summary>Everything printed on an invoice or credit note, already in the document language.</summary>
    public sealed record InvoicePdfModel(
        InvoiceKind Kind,
        string Number,
        DateTime IssuedAtLocal,
        string Language,
        string Currency,
        string CustomerName,
        string? CustomerEmail,
        string? CardLast4,
        IReadOnlyList<string> References,
        IReadOnlyList<InvoiceLine> Lines,
        decimal VatRate,
        decimal AmountExclVat,
        decimal VatAmount,
        decimal AmountInclVat,
        string? PaymentNote,
        string? CreditedInvoiceNumber = null,
        string? Reason = null);

    /// <summary>
    /// Fonts of the billing documents: Inter for Latin text and Noto Sans Arabic (SIL OFL, Assets/Fonts) for
    /// Arabic, registered once. Each text falls back from one to the other, so a name in Arabic prints on a French
    /// invoice and digits print on an Arabic one.
    /// </summary>
    public static class BillingFonts
    {
        public const string Latin = "Inter";
        public const string Arabic = "Noto Sans Arabic";

        private static readonly Lazy<bool> Registered = new(() =>
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
            foreach (var weight in new[] { "Regular", "Medium", "SemiBold", "Bold" })
                Register(Latin, Path.Combine(folder, $"Inter-{weight}.ttf"));
            foreach (var weight in new[] { "Regular", "Bold" })
                Register(Arabic, Path.Combine(folder, $"NotoSansArabic-{weight}.ttf"));
            return true;
        });

        public static void Ensure()
        {
            // Same license as the API sets at startup (ServiceRegistration.ConfigureQuestPDF), for hosts that skip it.
            QuestPDF.Settings.License ??= LicenseType.Community;
            _ = Registered.Value;
        }

        /// <summary>Font families in fallback order for a document language.</summary>
        public static string[] For(string language) =>
            BillingTexts.IsRightToLeft(language) ? new[] { Arabic, Latin } : new[] { Latin, Arabic };

        private static void Register(string family, string path)
        {
            using var stream = File.OpenRead(path);
            FontManager.RegisterFontWithCustomName(family, stream);
        }
    }

    /// <summary>
    /// Renders a legal invoice or credit note (A4, in memory): seller with its legal identifiers (ICE, IF, RC, TP,
    /// CNSS, from the CompanyInformations configuration), customer, lines, VAT summary and totals. Arabic pages are
    /// laid out right to left.
    /// </summary>
    public static class InvoicePdf
    {
        private const string Ink = "#0A1A3A";
        private const string Muted = "#5B6680";
        private const string Border = "#DFE5F0";
        private const string Soft = "#F3F6FB";
        private const string Accent = "#E5B223";

        public static byte[] Render(InvoicePdfModel model, CompanyInformations company)
        {
            BillingFonts.Ensure();
            var lang = model.Language;
            var rtl = BillingTexts.IsRightToLeft(lang);
            string L(string key) => BillingTexts.Get(lang, key);
            string Money(decimal amount) => BillingTexts.Money(amount, lang, model.Currency);
            var culture = BillingTexts.NumberCulture(lang);
            var logo = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "eveon-logo.svg");
            var title = model.Kind == InvoiceKind.CreditNote ? L("CreditNote") : L("Invoice");

            IContainer End(IContainer c) => rtl ? c.AlignLeft() : c.AlignRight();

            var document = Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(36);
                page.MarginHorizontal(42);
                page.PageColor(Colors.White);
                if (rtl)
                    page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily(BillingFonts.For(lang)).FontSize(9).FontColor(Ink).LineHeight(1.3f));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Height(34).Svg(SvgImage.FromFile(logo)).FitHeight();
                        row.RelativeItem().Column(col =>
                        {
                            End(col.Item()).Text(title).FontSize(20).Bold();
                            End(col.Item()).Text(t =>
                            {
                                t.Span(L("Number") + " ").FontColor(Muted);
                                t.Span(model.Number).SemiBold();
                            });
                            End(col.Item()).Text(t =>
                            {
                                t.Span(L("IssuedOn") + " ").FontColor(Muted);
                                t.Span(model.IssuedAtLocal.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).SemiBold();
                            });
                        });
                    });
                    header.Item().PaddingTop(12).Height(3).Background(Accent);
                });

                page.Content().PaddingTop(18).Column(content =>
                {
                    content.Spacing(16);

                    content.Item().Row(row =>
                    {
                        row.Spacing(14);
                        Box(row.RelativeItem(), L("Seller"), col =>
                        {
                            col.Item().Text(Or(company.CompanyName, "EVEON")).FontSize(11).SemiBold();
                            if (!string.IsNullOrWhiteSpace(company.Address))
                                col.Item().Text(company.Address);
                            var contact = string.Join("  ·  ", new[] { company.FixPhone, company.EmailContactAddress, company.Website }
                                .Where(v => !string.IsNullOrWhiteSpace(v)));
                            if (contact.Length > 0)
                                col.Item().Text(contact).FontColor(Muted).DirectionFromLeftToRight();
                            var ids = string.Join("  ·  ", new[] { ("ICE", company.ICE), ("IF", company.IF), ("RC", company.RC), ("TP", company.TP), ("CNSS", company.CNSS) }
                                .Where(p => !string.IsNullOrWhiteSpace(p.Item2)).Select(p => $"{p.Item1} {p.Item2}"));
                            if (ids.Length > 0)
                                col.Item().PaddingTop(3).Text(ids).FontSize(8).FontColor(Muted).DirectionFromLeftToRight();
                        });
                        Box(row.RelativeItem(), model.Kind == InvoiceKind.CreditNote ? L("CreditedTo") : L("BilledTo"), col =>
                        {
                            col.Item().Text(Or(model.CustomerName, "—")).FontSize(11).SemiBold();
                            if (!string.IsNullOrWhiteSpace(model.CustomerEmail))
                                col.Item().Text(model.CustomerEmail).FontColor(Muted);
                            if (!string.IsNullOrWhiteSpace(model.CardLast4))
                                col.Item().Text($"{L("Card")} •••• {model.CardLast4}").FontColor(Muted);
                        });
                    });

                    if (model.CreditedInvoiceNumber != null || model.References.Count > 0 || model.Reason != null)
                        content.Item().Background(Soft).Padding(10).Column(col =>
                        {
                            if (model.CreditedInvoiceNumber != null)
                                col.Item().Text(BillingTexts.Format(lang, "CreditsInvoice", model.CreditedInvoiceNumber)).SemiBold();
                            foreach (var reference in model.References)
                                col.Item().Text(reference);
                            if (!string.IsNullOrWhiteSpace(model.Reason))
                                col.Item().Text(t =>
                                {
                                    t.Span(L("Reason") + " : ").FontColor(Muted);
                                    t.Span(model.Reason);
                                });
                        });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn(2.2f);
                        });
                        table.Header(h =>
                        {
                            HeaderCell(h.Cell(), L("Description"), false);
                            HeaderCell(h.Cell(), L("Quantity"), true);
                            HeaderCell(h.Cell(), L("UnitPrice"), true);
                            HeaderCell(h.Cell(), L("Amount"), true);
                        });
                        foreach (var line in model.Lines)
                        {
                            IContainer Cell() => table.Cell().BorderBottom(1).BorderColor(Border).PaddingVertical(7).PaddingHorizontal(8);
                            Cell().Column(col =>
                            {
                                col.Item().Text(line.Description).Medium();
                                if (!string.IsNullOrWhiteSpace(line.Detail))
                                    col.Item().Text(line.Detail).FontSize(7.5f).FontColor(Muted);
                            });
                            End(Cell()).AlignMiddle().Text(line.Quantity);
                            End(Cell()).AlignMiddle().Text(line.UnitPrice ?? "—").FontColor(Muted);
                            End(Cell()).AlignMiddle().Text(Money(line.Amount)).SemiBold();
                        }
                    });

                    content.Item().Row(row =>
                    {
                        row.Spacing(16);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().PaddingBottom(4).Text(L("VatSummary")).SemiBold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(1.4f);
                                });
                                table.Header(h =>
                                {
                                    HeaderCell(h.Cell(), L("VatRate"), false, light: true);
                                    HeaderCell(h.Cell(), L("BaseExclVat"), true, light: true);
                                    HeaderCell(h.Cell(), L("Vat"), true, light: true);
                                    HeaderCell(h.Cell(), L("Amount"), true, light: true);
                                });
                                IContainer Cell() => table.Cell().BorderBottom(1).BorderColor(Border).PaddingVertical(5).PaddingHorizontal(6);
                                Cell().Text($"{(model.VatRate * 100).ToString("0.##", culture)} %");
                                End(Cell()).Text(Money(model.AmountExclVat));
                                End(Cell()).Text(Money(model.VatAmount));
                                End(Cell()).Text(Money(model.AmountInclVat));
                            });
                            if (!string.IsNullOrWhiteSpace(model.PaymentNote))
                                col.Item().PaddingTop(10).Column(payment =>
                                {
                                    payment.Item().Text(L("Payment")).FontSize(7.5f).SemiBold().FontColor(Muted);
                                    payment.Item().Text(model.PaymentNote);
                                });
                        });
                        row.ConstantItem(200).Column(col =>
                        {
                            Total(col, L("TotalExclVat"), Money(model.AmountExclVat));
                            Total(col, $"{L("TotalVat")} ({(model.VatRate * 100).ToString("0.##", culture)} %)", Money(model.VatAmount));
                            col.Item().PaddingTop(6).Background(Soft).BorderTop(2).BorderColor(Accent).Padding(8).Row(r =>
                            {
                                r.RelativeItem().AlignMiddle().Text(model.Kind == InvoiceKind.CreditNote ? L("TotalCredited") : L("TotalInclVat")).SemiBold();
                                r.AutoItem().AlignMiddle().Text(Money(model.AmountInclVat)).FontSize(13).Bold();
                            });
                        });
                    });

                    content.Item().Text(L("PricesNote")).FontSize(7.5f).FontColor(Muted);
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Border);
                    footer.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span(Or(company.CompanyName, "EVEON")).SemiBold();
                            var ids = string.Join(" · ", new[] { ("ICE", company.ICE), ("IF", company.IF), ("RC", company.RC) }
                                .Where(p => !string.IsNullOrWhiteSpace(p.Item2)).Select(p => $"{p.Item1} {p.Item2}"));
                            if (ids.Length > 0)
                                t.Span("  ·  " + ids).FontColor(Muted);
                            t.DefaultTextStyle(x => x.FontSize(7.5f));
                        });
                        row.AutoItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Muted));
                            t.Span(model.Number + "  ·  " + L("Page") + " ");
                            t.CurrentPageNumber();
                            t.Span(" / ");
                            t.TotalPages();
                        });
                    });
                });
            }));

            document.WithMetadata(new DocumentMetadata
            {
                Title = $"{title} {model.Number}",
                Author = Or(company.CompanyName, "EVEON"),
                Subject = model.Number
            });
            return document.GeneratePdf();

            void Box(IContainer container, string label, Action<ColumnDescriptor> body) =>
                container.Border(1).BorderColor(Border).Padding(10).Column(col =>
                {
                    col.Item().PaddingBottom(4).Text(label).FontSize(7).SemiBold().FontColor(Muted).LetterSpacing(0.06f);
                    body(col);
                });

            void HeaderCell(IContainer container, string text, bool end, bool light = false)
            {
                var cell = container.Background(light ? Soft : Ink).PaddingVertical(6).PaddingHorizontal(light ? 6 : 8);
                (end ? End(cell) : cell).Text(text).FontSize(7).SemiBold().FontColor(light ? Muted : Colors.White);
            }

            void Total(ColumnDescriptor col, string label, string value) =>
                col.Item().PaddingVertical(3).PaddingHorizontal(8).Row(r =>
                {
                    r.RelativeItem().Text(label).FontColor(Muted);
                    r.AutoItem().Text(value).Medium();
                });
        }

        private static string Or(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
