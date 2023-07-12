using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class InvoiceDTO
    {
        public string OrderNumber { get; set; }
        public string BilledTo { get; set; }
        public string PayTo { get; set; }
        public string PaymentMethod { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string CardID { get; set; }
        public string Date { get; set; }
    }
}

