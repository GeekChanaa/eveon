using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class RechargeOrderDto
    {
        [Range(1, int.MaxValue)]
        public int CardID { get; set; }
        [Range(0.01, 100000)]
        public double RechargeAmount { get; set; }
        // Raw card data (PAN/CVV) is never accepted here: pay with a saved, tokenized DebitCardID.
        [Range(1, int.MaxValue)]
        public int? DebitCardID { get; set; }
        public int UserID { get; set; }
        public bool SaveCard { get; set; }
        // Mock-only: lets the client exercise each payment outcome without a gateway.
        public RechargeOrderStatus? MockPaymentStatus { get; set; }
    }
}
