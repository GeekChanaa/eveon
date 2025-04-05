
namespace VoltaXApi.Models;


public class InvoiceData
{
    public string InvoiceNumber { get; set; }
    public string BilledTo { get; set; }
    public string CardNumber { get; set; }
    public string Date { get; set; }
    public double TotalAmount { get; set; }
    public double AmountHT { get; set; }
    public double VAT { get; set; }

}