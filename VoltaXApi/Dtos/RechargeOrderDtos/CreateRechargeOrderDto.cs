using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CreateRechargeOrderDto
    {
        public int CardID { get; set; }
        public RechargeOrderStatus Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime? RechargeDate { get; set; } = DateTime.Now;
    }
}