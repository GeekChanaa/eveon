namespace VoltaXApi.Services;
public interface IInvoiceGeneratorService<TData>
{
    /// <summary>Renders the PDF in memory (never through a file: concurrent requests must not share one).</summary>
    byte[] GenerateInvoice(TData data);
}
