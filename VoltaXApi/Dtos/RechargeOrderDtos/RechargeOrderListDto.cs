using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class RechargeOrderListDto
    {
        public int ID { get; set; }
        public int? CardID { get; set; }
        public decimal Amount { get; set; }
        public string CardNumber { get; set; }
        public string UserName { get; set; }
        public RechargeOrderStatus Status { get; set; }
        public DateTime RechargeDate { get; set; }
    }
}