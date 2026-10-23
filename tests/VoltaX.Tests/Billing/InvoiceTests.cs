using System.Text;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Billing;

public class InvoiceTests
{
    public InvoiceTests() => QuestPDF.Settings.License = LicenseType.Community;

    private static RechargeOrderInvoiceGenerator OrderInvoices() => new(
        Options.Create(new CompanyInformations { CompanyName = "EVEON", Address = "Casablanca", EmailContactAddress = "contact@eveon.ma" }),
        new ApiHostEnvironment());

    [Fact]
    public async Task Order_invoices_are_rendered_in_memory_per_request()
    {
        var generator = OrderInvoices();
        var a = new InvoiceData { InvoiceNumber = "1", BilledTo = "Alice", CardNumber = "1111", Date = "20 Oct 2026", TotalAmount = 120m, AmountHT = 100m, VAT = 20m };
        var b = new InvoiceData { InvoiceNumber = "2", BilledTo = "Bob", CardNumber = "2222", Date = "20 Oct 2026", TotalAmount = 60m, AmountHT = 50m, VAT = 10m };
        var before = Directory.GetFiles(Environment.CurrentDirectory, "*.pdf").Length;

        var pdfs = await Task.WhenAll(Task.Run(() => generator.GenerateInvoice(a)), Task.Run(() => generator.GenerateInvoice(b)));

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
