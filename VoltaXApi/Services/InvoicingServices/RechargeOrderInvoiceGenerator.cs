
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using VoltaXApi.Models;

namespace VoltaXApi.Services;

public class RechargeOrderInvoiceGenerator : IInvoiceGeneratorService<InvoiceData>
{
    private readonly IOptions<CompanyInformations> _companyInformations;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RechargeOrderInvoiceGenerator(
        IOptions<CompanyInformations> companyInformations,
        IWebHostEnvironment webHostEnvironment)
    {
        _companyInformations = companyInformations;
        _webHostEnvironment = webHostEnvironment;
    }

    public byte[] GenerateInvoice(InvoiceData data, string fileName)
    {
        var document = new InvoiceDocument(data, _companyInformations.Value,_webHostEnvironment);
        document.GeneratePdf(fileName);

        return File.ReadAllBytes(fileName);
    }
}