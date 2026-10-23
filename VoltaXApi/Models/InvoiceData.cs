
namespace VoltaXApi.Models;


public class InvoiceData
{
    public string InvoiceNumber { get; set; }
    public string BilledTo { get; set; }
    public string CardNumber { get; set; }
    public string Date { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountHT { get; set; }
    public decimal VAT { get; set; }

}