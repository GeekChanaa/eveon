using System.Text;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services;
using VoltaXApi.Services.Billing;

namespace VoltaX.Tests.Billing;

public class InvoiceTests
{
    private static InvoicePdfModel Model(string number, string customer, decimal total) => new(InvoiceKind.Invoice, number, Seeds.Now, "fr", "MAD",
        customer, null, "1111", Array.Empty<string>(), new[] { new InvoiceLine("Rechargement", null, "1", null, total) }, 0.2m,
        CostCalculator.SplitVat(total, 0.2m).ExclVat, CostCalculator.SplitVat(total, 0.2m).Vat, total, null);

    [Fact]
    public async Task Invoices_are_rendered_in_memory_per_request()
    {
        var company = new CompanyInformations { CompanyName = "EVEON", Address = "Casablanca", EmailContactAddress = "contact@eveon.ma" };
        var before = Directory.GetFiles(Environment.CurrentDirectory, "*.pdf").Length;

        var pdfs = await Task.WhenAll(
            Task.Run(() => InvoicePdf.Render(Model("EV-2026-000001", "Alice", 120m), company)),
            Task.Run(() => InvoicePdf.Render(Model("EV-2026-000002", "Bob", 60m), company)));

        Assert.All(pdfs, pdf => Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4)));
        Assert.NotEqual(pdfs[0], pdfs[1]);
        Assert.Equal(before, Directory.GetFiles(Environment.CurrentDirectory, "*.pdf").Length);
    }

    [Theory]
    [InlineData(120, 100, 20)]
    [InlineData(100, 83.33, 16.67)]
    [InlineData(0.01, 0.01, 0)]
    public void Vat_of_a_paid_amount_is_the_part_above_the_excluding_vat_amount(double paid, double excl, double vat)
    {
        var split = CostCalculator.SplitVat((decimal)paid, 0.2m);

        Assert.Equal((decimal)excl, split.ExclVat);
        Assert.Equal((decimal)vat, split.Vat);
    }
}
