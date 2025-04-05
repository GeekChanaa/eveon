namespace VoltaXApi.Services;
public interface IInvoiceGeneratorService<TData>
{
    byte[] GenerateInvoice(TData data, string fileName);
}
